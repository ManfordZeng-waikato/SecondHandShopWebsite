using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.Contracts.Sales;

/// <summary>
/// Input to revert a sold product back to Available. The current sale record is marked
/// <c>Cancelled</c> with the supplied reason — its buyer/price/time fields are preserved.
/// </summary>
public sealed record RevertProductSaleRequest(
    Guid ProductId,
    SaleCancellationReason Reason,
    string? CancellationNote,
    Guid? AdminUserId);
