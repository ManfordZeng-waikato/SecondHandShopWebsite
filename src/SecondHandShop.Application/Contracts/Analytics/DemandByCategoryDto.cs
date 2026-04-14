namespace SecondHandShop.Application.Contracts.Analytics;

public sealed record DemandByCategoryDto(
    Guid CategoryId,
    string CategoryName,
    int InquiryCount,
    int SoldCount,
    decimal ConversionRate,
    decimal HeatScore);
