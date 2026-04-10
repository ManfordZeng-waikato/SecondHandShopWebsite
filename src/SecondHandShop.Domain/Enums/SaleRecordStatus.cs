namespace SecondHandShop.Domain.Enums;

/// <summary>
/// Lifecycle state of a single sale record.
/// A product may have many <see cref="SaleRecordStatus.Cancelled"/> records in its history,
/// but at most one <see cref="SaleRecordStatus.Completed"/> record at any given time.
/// </summary>
public enum SaleRecordStatus : byte
{
    Completed = 1,
    Cancelled = 2
}
