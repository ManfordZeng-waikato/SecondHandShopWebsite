namespace SecondHandShop.Application.Abstractions.Messaging;

public interface IEmailSender
{
    Task SendInquiryAsync(InquiryEmailMessage message, CancellationToken cancellationToken = default);
}
