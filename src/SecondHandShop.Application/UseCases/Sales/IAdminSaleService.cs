using SecondHandShop.Application.Contracts.Sales;

namespace SecondHandShop.Application.UseCases.Sales;

public interface IAdminSaleService
{
    Task<ProductSaleDto?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductSaleDto> SaveAsync(SaveProductSaleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerSaleItemDto>> ListByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}
