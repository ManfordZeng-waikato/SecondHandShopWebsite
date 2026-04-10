using SecondHandShop.Application.Contracts.Sales;

namespace SecondHandShop.Application.UseCases.Sales;

public interface IAdminSaleService
{
    /// <summary>
    /// Returns the currently active (Completed) sale for the product, or null if not sold.
    /// </summary>
    Task<ProductSaleDto?> GetCurrentSaleAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Full sale history for a product, including cancelled records. Most recent first.
    /// </summary>
    Task<IReadOnlyList<ProductSaleDto>> GetSaleHistoryAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new Completed sale and transition the product to Sold. Fails if the product
    /// is already Sold — reverts must go through <see cref="RevertSaleAsync"/> first.
    /// </summary>
    Task<ProductSaleDto> MarkAsSoldAsync(MarkProductSoldRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transition a sold product back to Available while preserving the sale row as Cancelled.
    /// </summary>
    Task RevertSaleAsync(RevertProductSaleRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerSaleItemDto>> ListByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}
