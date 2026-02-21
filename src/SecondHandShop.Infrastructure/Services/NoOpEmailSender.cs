using Microsoft.Extensions.Logging;
using SecondHandShop.Application.Abstractions.Messaging;

namespace SecondHandShop.Infrastructure.Services;

public class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    public Task SendInquiryAsync(InquiryEmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Inquiry email queued in placeholder sender. InquiryId={InquiryId}, ProductId={ProductId}, ProductSlug={ProductSlug}",
            message.InquiryId,
            message.ProductId,
            message.ProductSlug);

        return Task.CompletedTask;
    }
}
