using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Application.Contracts.Sales;
using SecondHandShop.Application.UseCases.Customers;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UseCases.Sales;

public class AdminSaleService(
    IProductRepository productRepository,
    IProductSaleRepository productSaleRepository,
    ICustomerRepository customerRepository,
    IInquiryRepository inquiryRepository,
    ICustomerResolutionService customerResolutionService,
    IUnitOfWork unitOfWork,
    IClock clock) : IAdminSaleService
{
    private static readonly DateTime SoldAtUtcLowerBoundUtc = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan SoldAtUtcMaxFutureSkew = TimeSpan.FromDays(1);
    public async Task<ProductSaleDto?> GetCurrentSaleAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var sale = await productSaleRepository.GetCurrentByProductIdAsync(productId, cancellationToken);
        return sale is null ? null : MapToDto(sale);
    }

    public async Task<IReadOnlyList<ProductSaleDto>> GetSaleHistoryAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            throw new KeyNotFoundException($"Product '{productId}' not found.");
        }

        var sales = await productSaleRepository.ListHistoryByProductIdAsync(productId, cancellationToken);
        return sales.Select(MapToDto).ToList();
    }

    public async Task<ProductSaleDto> MarkAsSoldAsync(
        MarkProductSoldRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FinalSoldPrice < 0)
        {
            throw new ValidationException("Final sold price cannot be negative.");
        }

        var utcNow = clock.UtcNow;
        var soldAtUtc = NormalizeSoldAtToUtc(request.SoldAtUtc);
        ValidateSoldAtUtc(soldAtUtc, utcNow);

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{request.ProductId}' not found.");

        if (product.Status == ProductStatus.Sold)
        {
            throw new ConflictException(
                "Product is already sold. Revert the current sale before marking it sold again.");
        }

        var paymentMethod = ParsePaymentMethod(request.PaymentMethod);

        // Resolve CustomerId: explicit value, or auto-resolve from buyer info.
        var customerId = request.CustomerId;
        if (customerId.HasValue)
        {
            var customer = await customerRepository.GetByIdAsync(customerId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Customer '{customerId}' not found.");
            _ = customer;
        }
        else if (HasBuyerContact(request))
        {
            var resolved = await customerResolutionService.GetOrCreateCustomerAsync(
                request.BuyerName,
                request.BuyerEmail,
                request.BuyerPhone,
                CustomerSource.Sale,
                cancellationToken);
            customerId = resolved.Id;
        }

        if (request.InquiryId.HasValue)
        {
            var inquiry = await inquiryRepository.GetByIdAsync(request.InquiryId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Inquiry '{request.InquiryId}' not found.");
            if (inquiry.ProductId != request.ProductId)
            {
                throw new ValidationException("The selected inquiry does not belong to this product.");
            }
        }

        // Domain aggregate atomically creates the ProductSale and updates Product state.
        var sale = product.MarkAsSold(
            finalSoldPrice: request.FinalSoldPrice,
            soldAtUtc: soldAtUtc,
            adminUserId: request.AdminUserId,
            utcNow: utcNow,
            customerId: customerId,
            inquiryId: request.InquiryId,
            buyerName: request.BuyerName,
            buyerPhone: request.BuyerPhone,
            buyerEmail: request.BuyerEmail,
            paymentMethod: paymentMethod,
            notes: request.Notes);

        await productSaleRepository.AddAsync(sale, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(sale);
    }

    public async Task RevertSaleAsync(
        RevertProductSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{request.ProductId}' not found.");

        if (product.Status != ProductStatus.Sold || product.CurrentSaleId is null)
        {
            throw new ConflictException("Only sold products can be reverted to available.");
        }

        var currentSale = await productSaleRepository.GetByIdAsync(product.CurrentSaleId.Value, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Product '{product.Id}' points to missing sale '{product.CurrentSaleId}'.");

        product.RevertSoldToAvailable(
            currentSale: currentSale,
            reason: request.Reason,
            cancellationNote: request.CancellationNote,
            adminUserId: request.AdminUserId,
            utcNow: clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerSaleItemDto>> ListByCustomerIdAsync(
        Guid customerId, CancellationToken cancellationToken = default)
    {
        return await productSaleRepository.ListByCustomerIdAsync(customerId, cancellationToken);
    }

    private static DateTime NormalizeSoldAtToUtc(DateTime soldAtUtc)
    {
        return soldAtUtc.Kind switch
        {
            DateTimeKind.Utc => soldAtUtc,
            DateTimeKind.Local => soldAtUtc.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(soldAtUtc, DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(soldAtUtc, DateTimeKind.Utc),
        };
    }

    private static void ValidateSoldAtUtc(DateTime soldAtUtc, DateTime utcNow)
    {
        if (soldAtUtc < SoldAtUtcLowerBoundUtc)
        {
            throw new ValidationException(
                $"Sold time must be on or after {SoldAtUtcLowerBoundUtc:yyyy-MM-dd} (UTC).");
        }

        if (soldAtUtc > utcNow.Add(SoldAtUtcMaxFutureSkew))
        {
            throw new ValidationException("Sold time cannot be more than one day in the future (UTC).");
        }
    }

    private static bool HasBuyerContact(MarkProductSoldRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.BuyerEmail)
               || !string.IsNullOrWhiteSpace(request.BuyerPhone);
    }

    private static PaymentMethod? ParsePaymentMethod(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<PaymentMethod>(value, true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new ValidationException($"Unsupported payment method '{value}'.");
        }

        return parsed;
    }

    private static ProductSaleDto MapToDto(ProductSale sale)
    {
        return new ProductSaleDto(
            sale.Id,
            sale.ProductId,
            sale.CustomerId,
            sale.InquiryId,
            sale.ListedPriceAtSale,
            sale.FinalSoldPrice,
            sale.BuyerName,
            sale.BuyerPhone,
            sale.BuyerEmail,
            sale.SoldAtUtc,
            sale.PaymentMethod?.ToString(),
            sale.Notes,
            sale.Status.ToString(),
            sale.CancelledAtUtc,
            sale.CancellationReason?.ToString(),
            sale.CancellationNote,
            sale.CreatedByAdminUserId,
            sale.CreatedAt,
            sale.UpdatedAt);
    }
}
