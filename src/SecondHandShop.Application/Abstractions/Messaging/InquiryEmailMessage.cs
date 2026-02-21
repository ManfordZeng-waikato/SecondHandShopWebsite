namespace SecondHandShop.Application.Abstractions.Messaging;

public sealed record InquiryEmailMessage(
    Guid InquiryId,
    Guid ProductId,
    string ProductTitle,
    string ProductSlug,
    string? CustomerName,
    string? Email,
    string? PhoneNumber,
    string Message);
