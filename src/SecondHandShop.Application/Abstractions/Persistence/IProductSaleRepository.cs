using SecondHandShop.Application.Contracts.Sales;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.Abstractions.Persistence;

public interface IProductSaleRepository
{
    Task<ProductSale?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerSaleItemDto>> ListByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductSale sale, CancellationToken cancellationToken = default);
}
