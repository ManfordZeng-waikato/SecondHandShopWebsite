using Microsoft.Extensions.Configuration;

namespace SecondHandShop.Infrastructure.Services;

public class SmtpEmailOptions
{
    public const string SectionName = "Email:Smtp";

    public bool Enabled { get; init; }
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool UseSsl { get; init; } = true;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "SecondHandShop";
    public string AdminInboxEmail { get; init; } = string.Empty;
    public string FrontendBaseUrl { get; init; } = string.Empty;

    public static SmtpEmailOptions FromConfiguration(IConfiguration configuration)
    {
        return new SmtpEmailOptions
        {
            Enabled = ParseBool(configuration[$"{SectionName}:Enabled"], defaultValue: false),
            Host = configuration[$"{SectionName}:Host"] ?? string.Empty,
            Port = ParseInt(configuration[$"{SectionName}:Port"], defaultValue: 587),
            UseSsl = ParseBool(configuration[$"{SectionName}:UseSsl"], defaultValue: true),
            Username = configuration[$"{SectionName}:Username"] ?? string.Empty,
            Password = configuration[$"{SectionName}:Password"] ?? string.Empty,
            FromEmail = configuration[$"{SectionName}:FromEmail"] ?? string.Empty,
            FromName = configuration[$"{SectionName}:FromName"] ?? "SecondHandShop",
            AdminInboxEmail = configuration[$"{SectionName}:AdminInboxEmail"] ?? string.Empty,
            FrontendBaseUrl = configuration[$"{SectionName}:FrontendBaseUrl"] ?? string.Empty
        };
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static bool ParseBool(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
