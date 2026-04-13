namespace SecondHandShop.Application.Abstractions.Messaging;

public interface IEmailSender
{
    Task SendInquiryAsync(InquiryEmailMessage message, CancellationToken cancellationToken = default);
    Task SendAdminLoginNotificationAsync(
        AdminLoginNotificationMessage message,
        CancellationToken cancellationToken = default);
}
