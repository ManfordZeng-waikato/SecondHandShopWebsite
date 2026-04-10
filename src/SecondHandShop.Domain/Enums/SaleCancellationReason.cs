namespace SecondHandShop.Domain.Enums;

/// <summary>
/// Why a completed sale was reverted back to the available pool.
/// Free-form context goes into <c>CancellationNote</c>; this enum is for reporting/filtering.
/// </summary>
public enum SaleCancellationReason : byte
{
    BuyerBackedOut = 1,
    PaymentFailed = 2,
    AdminMistake = 3,
    OfflineCancelled = 4,
    Other = 99
}
