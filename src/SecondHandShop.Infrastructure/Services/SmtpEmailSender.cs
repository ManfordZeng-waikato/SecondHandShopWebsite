using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Logging;
using SecondHandShop.Application.Abstractions.Messaging;

namespace SecondHandShop.Infrastructure.Services;

public sealed class SmtpEmailSender(
    SmtpEmailOptions options,
    SmtpConnectionLease connectionLease,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private static readonly TimeZoneInfo NzTimeZone = ResolveNewZealandTimeZone();

    public async Task SendInquiryAsync(InquiryEmailMessage message, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var mailMessage = BuildInquiryMessage(message);

        logger.LogInformation(
            "Sending inquiry email. ProductId={ProductId}, InquiryId={InquiryId}, To={To}",
            message.ProductId,
            message.InquiryId,
            options.AdminInboxEmail);

        await connectionLease.SendAsync(mailMessage, cancellationToken);
    }

    public async Task SendAdminLoginNotificationAsync(
        AdminLoginNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var mailMessage = BuildAdminLoginNotificationMessage(message);

        logger.LogInformation(
            "Sending admin login notification email. AdminUserId={AdminUserId}, UserName={UserName}, To={To}",
            message.AdminUserId,
            message.UserName,
            options.AdminInboxEmail);

        await connectionLease.SendAsync(mailMessage, cancellationToken);
    }

    private MimeMessage BuildInquiryMessage(InquiryEmailMessage message)
    {
        var productUrl = BuildProductUrl(message.ProductSlug);
        var subject = $"[Inquiry] {message.ProductTitle}";

        var textBody = $"""
            A customer submitted a new inquiry.

            Product:
            - Title: {message.ProductTitle}
            - URL: {productUrl}

            Customer:
            - Name: {message.CustomerName ?? "(not provided)"}
            - Email: {message.Email ?? "(not provided)"}
            - Phone: {message.PhoneNumber ?? "(not provided)"}

            Message:
            {message.Message}
            """;

        return CreateTextMessage(subject, textBody);
    }

    private MimeMessage BuildAdminLoginNotificationMessage(AdminLoginNotificationMessage message)
    {
        var localOccurredAt = ConvertUtcToNewZealandTime(message.OccurredAtUtc);
        var subject = $"[Admin Login] {message.UserName}";
        var textBody = $"""
            An administrator signed in successfully.

            Admin:
            - Display name: {message.DisplayName}
            - Username: {message.UserName}
            - Email: {message.Email}
            - User ID: {message.AdminUserId}

            Session:
            - Time (NZ): {localOccurredAt:yyyy-MM-dd HH:mm:ss} {GetNewZealandTimeZoneAbbreviation(localOccurredAt)}
            - Source: {DescribeSourceIpAddress(message.SourceIpAddress)}

            If this login was not expected, rotate the password immediately and revoke active sessions.
            """;

        return CreateTextMessage(subject, textBody);
    }

    private MimeMessage CreateTextMessage(string subject, string textBody)
    {
        var mailMessage = new MimeMessage();
        mailMessage.From.Add(new MailboxAddress(options.FromName, options.FromEmail));
        mailMessage.To.Add(MailboxAddress.Parse(options.AdminInboxEmail));
        mailMessage.Subject = subject;
        mailMessage.Body = new TextPart("plain")
        {
            Text = textBody
        };

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

    private static DateTimeOffset ConvertUtcToNewZealandTime(DateTime utcTime)
    {
        var utc = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTime(new DateTimeOffset(utc), NzTimeZone);
    }

    private static string GetNewZealandTimeZoneAbbreviation(DateTimeOffset localTime)
        => localTime.Offset == TimeSpan.FromHours(13) ? "NZDT" : "NZST";

    private static string DescribeSourceIpAddress(string? sourceIpAddress)
    {
        if (string.IsNullOrWhiteSpace(sourceIpAddress))
            return "(not available)";

        var trimmed = sourceIpAddress.Trim();
        if (!System.Net.IPAddress.TryParse(trimmed, out var ipAddress))
            return trimmed;

        if (System.Net.IPAddress.IsLoopback(ipAddress))
            return $"{trimmed} (localhost / local development)";

        if (IsPrivateAddress(ipAddress))
            return $"{trimmed} (private network)";

        return trimmed;
    }

    private static bool IsPrivateAddress(System.Net.IPAddress ipAddress)
    {
        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = ipAddress.GetAddressBytes();
            return bytes[0] switch
            {
                10 => true,
                172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
                192 when bytes[1] == 168 => true,
                _ => false
            };
        }

        return ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal || ipAddress.IsIPv6UniqueLocal;
    }

    private static TimeZoneInfo ResolveNewZealandTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");
        }
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
