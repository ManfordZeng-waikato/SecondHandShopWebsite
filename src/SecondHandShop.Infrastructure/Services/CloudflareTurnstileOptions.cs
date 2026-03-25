using Microsoft.Extensions.Configuration;

namespace SecondHandShop.Infrastructure.Services;

public sealed class CloudflareTurnstileOptions
{
    public const string SectionName = "CloudflareTurnstile";
    public const string DefaultVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public string SiteKey { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public string VerifyUrl { get; init; } = DefaultVerifyUrl;

    public static CloudflareTurnstileOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        return new CloudflareTurnstileOptions
        {
            SiteKey = section["SiteKey"] ?? string.Empty,
            SecretKey = section["SecretKey"] ?? string.Empty,
            VerifyUrl = string.IsNullOrWhiteSpace(section["VerifyUrl"])
                ? DefaultVerifyUrl
                : section["VerifyUrl"]!
        };
    }

    public void ValidateForServer()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException($"{SectionName}:SecretKey is required.");
        }

        if (string.IsNullOrWhiteSpace(VerifyUrl))
        {
            throw new InvalidOperationException($"{SectionName}:VerifyUrl is required.");
        }
    }
}
