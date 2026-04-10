using SecondHandShop.Application.Contracts.Sales;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.Abstractions.Persistence;

public interface IProductSaleRepository
{
    /// <summary>
    /// The currently-active (Completed) sale for a product, or null if the product is not sold.
    /// </summary>
    Task<ProductSale?> GetCurrentByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a single sale record by id — needed by the revert flow so we can reload the
    /// current sale and pass it into the domain aggregate.
    /// </summary>
    Task<ProductSale?> GetByIdAsync(Guid saleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Full history for a product (completed + cancelled), most recent sold-at first.
    /// </summary>
    Task<IReadOnlyList<ProductSale>> ListHistoryByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sales belonging to a customer — only completed ones, for the customer profile view.
    /// </summary>
    Task<IReadOnlyList<CustomerSaleItemDto>> ListByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ProductSale sale, CancellationToken cancellationToken = default);
}
