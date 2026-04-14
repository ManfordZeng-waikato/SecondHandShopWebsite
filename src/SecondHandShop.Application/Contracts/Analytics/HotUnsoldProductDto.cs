namespace SecondHandShop.Application.Contracts.Analytics;

public sealed record HotUnsoldProductDto(
    Guid ProductId,
    string Title,
    string Slug,
    Guid CategoryId,
    string CategoryName,
    int InquiryCount,
    decimal ListedPrice,
    int DaysListed);
