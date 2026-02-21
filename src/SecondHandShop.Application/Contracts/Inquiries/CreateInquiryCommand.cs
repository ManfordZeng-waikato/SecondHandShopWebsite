namespace SecondHandShop.Application.Contracts.Inquiries;

public sealed record CreateInquiryCommand(
    Guid ProductId,
    string? CustomerName,
    string? Email,
    string? PhoneNumber,
    string Message);
