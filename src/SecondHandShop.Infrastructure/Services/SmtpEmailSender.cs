using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using SecondHandShop.Application.Abstractions.Messaging;

namespace SecondHandShop.Infrastructure.Services;

public class SmtpEmailSender(
    SmtpEmailOptions options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendInquiryAsync(InquiryEmailMessage message, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var mailMessage = BuildMailMessage(message);
        using var smtpClient = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.UseSsl,
            Credentials = new NetworkCredential(options.Username, options.Password)
        };

        logger.LogInformation(
            "Sending inquiry email. InquiryId={InquiryId}, ProductId={ProductId}, To={To}",
            message.InquiryId,
            message.ProductId,
            options.AdminInboxEmail);

        // SmtpClient has no cancellation-aware API, so cancellation is checked explicitly before sending.
        cancellationToken.ThrowIfCancellationRequested();
        await smtpClient.SendMailAsync(mailMessage);
    }

    private MailMessage BuildMailMessage(InquiryEmailMessage message)
    {
        var productUrl = BuildProductUrl(message.ProductSlug);
        var subject = $"[Inquiry] {message.ProductTitle}";

        var textBody = $"""
            A customer submitted a new inquiry.

            Product:
            - Title: {message.ProductTitle}
            - URL: {productUrl}
            - ProductId: {message.ProductId}

            Customer:
            - Name: {message.CustomerName ?? "(not provided)"}
            - Email: {message.Email ?? "(not provided)"}
            - Phone: {message.PhoneNumber ?? "(not provided)"}

            Message:
            {message.Message}

            InquiryId: {message.InquiryId}
            """;

        var mailMessage = new MailMessage
        {
            From = new MailAddress(options.FromEmail, options.FromName),
            Subject = subject,
            Body = textBody,
            IsBodyHtml = false
        };

        mailMessage.To.Add(options.AdminInboxEmail);
        return mailMessage;
    }

    private string BuildProductUrl(string productSlug)
    {
        if (string.IsNullOrWhiteSpace(options.FrontendBaseUrl))
        {
            return productSlug;
        }

        return $"{options.FrontendBaseUrl.TrimEnd('/')}/products/{productSlug}";
    }

    private void EnsureConfigured()
    {
        var missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Host)) missingFields.Add(nameof(options.Host));
        if (string.IsNullOrWhiteSpace(options.Username)) missingFields.Add(nameof(options.Username));
        if (string.IsNullOrWhiteSpace(options.Password)) missingFields.Add(nameof(options.Password));
        if (string.IsNullOrWhiteSpace(options.FromEmail)) missingFields.Add(nameof(options.FromEmail));
        if (string.IsNullOrWhiteSpace(options.AdminInboxEmail)) missingFields.Add(nameof(options.AdminInboxEmail));

        if (missingFields.Count > 0)
        {
            throw new InvalidOperationException(
                $"SMTP email sender is not fully configured. Missing: {string.Join(", ", missingFields)}");
        }
    }
}
