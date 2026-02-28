using Microsoft.Extensions.Configuration;

namespace SecondHandShop.Infrastructure.Services;

public sealed class R2Options
{
    public const string SectionName = "R2";

    public string AccountId { get; init; } = string.Empty;
    public string AccessKeyId { get; init; } = string.Empty;
    public string SecretAccessKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;

    /// <summary>
    /// Base URL of the Cloudflare Worker that serves images, e.g. https://img.example.com
    /// </summary>
    public string WorkerBaseUrl { get; init; } = string.Empty;

    public static R2Options FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        return new R2Options
        {
            AccountId = section["AccountId"] ?? string.Empty,
            AccessKeyId = section["AccessKeyId"] ?? string.Empty,
            SecretAccessKey = section["SecretAccessKey"] ?? string.Empty,
            BucketName = section["BucketName"] ?? string.Empty,
            WorkerBaseUrl = section["WorkerBaseUrl"] ?? string.Empty
        };
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AccountId))
        {
            throw new InvalidOperationException("R2:AccountId is required.");
        }

        if (string.IsNullOrWhiteSpace(AccessKeyId))
        {
            throw new InvalidOperationException("R2:AccessKeyId is required.");
        }

        if (string.IsNullOrWhiteSpace(SecretAccessKey))
        {
            throw new InvalidOperationException("R2:SecretAccessKey is required.");
        }

        if (string.IsNullOrWhiteSpace(BucketName))
        {
            throw new InvalidOperationException("R2:BucketName is required.");
        }

        if (string.IsNullOrWhiteSpace(WorkerBaseUrl))
        {
            throw new InvalidOperationException("R2:WorkerBaseUrl is required.");
        }
    }
}
