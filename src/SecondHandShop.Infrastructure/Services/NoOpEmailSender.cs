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

    public Task SendAdminLoginNotificationAsync(
        AdminLoginNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Admin login notification suppressed by placeholder sender. AdminUserId={AdminUserId}, UserName={UserName}, SourceIpAddress={SourceIpAddress}, OccurredAtUtc={OccurredAtUtc}",
            message.AdminUserId,
            message.UserName,
            message.SourceIpAddress ?? "(unknown)",
            message.OccurredAtUtc);

        return Task.CompletedTask;
    }
}
