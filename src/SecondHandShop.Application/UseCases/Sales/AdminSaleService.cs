using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
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
    public async Task<ProductSaleDto?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var sale = await productSaleRepository.GetByProductIdAsync(productId, cancellationToken);
        return sale is null ? null : MapToDto(sale);
    }

    public async Task<ProductSaleDto> SaveAsync(SaveProductSaleRequest request, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{request.ProductId}' not found.");

        if (request.FinalSoldPrice < 0)
            throw new ArgumentException("Final sold price cannot be negative.");

        PaymentMethod? paymentMethod = null;
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var parsed) || !Enum.IsDefined(parsed))
                throw new ArgumentException($"Unsupported payment method '{request.PaymentMethod}'.");
            paymentMethod = parsed;
        }

        // Resolve CustomerId: use explicit value, or auto-resolve from buyer info
        var customerId = request.CustomerId;

        if (customerId.HasValue)
        {
            var customer = await customerRepository.GetByIdAsync(customerId.Value, cancellationToken);
            if (customer is null)
                throw new KeyNotFoundException($"Customer '{customerId}' not found.");
        }
        else if (HasBuyerContact(request))
        {
            // Auto-create or link customer from buyer info
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
            var inquiry = await inquiryRepository.GetByIdAsync(request.InquiryId.Value, cancellationToken);
            if (inquiry is null)
                throw new KeyNotFoundException($"Inquiry '{request.InquiryId}' not found.");
            if (inquiry.ProductId != request.ProductId)
                throw new ArgumentException("The selected inquiry does not belong to this product.");
        }

        var utcNow = clock.UtcNow;
        var existingSale = await productSaleRepository.GetByProductIdAsync(request.ProductId, cancellationToken);

        if (existingSale is not null)
        {
            existingSale.Update(
                request.FinalSoldPrice,
                request.SoldAtUtc,
                request.AdminUserId,
                utcNow,
                customerId,
                request.InquiryId,
                request.BuyerName,
                request.BuyerPhone,
                request.BuyerEmail,
                paymentMethod,
                request.Notes);
        }
        else
        {
            var sale = ProductSale.Create(
                request.ProductId,
                product.Price,
                request.FinalSoldPrice,
                request.SoldAtUtc,
                request.AdminUserId,
                utcNow,
                customerId,
                request.InquiryId,
                request.BuyerName,
                request.BuyerPhone,
                request.BuyerEmail,
                paymentMethod,
                request.Notes);

            await productSaleRepository.AddAsync(sale, cancellationToken);
            existingSale = sale;
        }

        // Update product status to Sold
        if (product.Status != ProductStatus.Sold)
        {
            product.MarkAsSold(request.AdminUserId, utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(existingSale);
    }

    public async Task<IReadOnlyList<CustomerSaleItemDto>> ListByCustomerIdAsync(
        Guid customerId, CancellationToken cancellationToken = default)
    {
        return await productSaleRepository.ListByCustomerIdAsync(customerId, cancellationToken);
    }

    private static bool HasBuyerContact(SaveProductSaleRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.BuyerEmail)
               || !string.IsNullOrWhiteSpace(request.BuyerPhone);
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
            sale.CreatedAt,
            sale.UpdatedAt);
    }
}
