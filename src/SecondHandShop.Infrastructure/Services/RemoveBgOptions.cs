using Microsoft.Extensions.Configuration;

namespace SecondHandShop.Infrastructure.Services;

public sealed class RemoveBgOptions
{
    public const string SectionName = "RemoveBg";

    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxRetries { get; init; } = 1;
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024; // 10 MB

    public static RemoveBgOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        return new RemoveBgOptions
        {
            ApiKey = section["ApiKey"] ?? string.Empty,
            TimeoutSeconds = int.TryParse(section["TimeoutSeconds"], out var t) ? t : 30,
            MaxRetries = int.TryParse(section["MaxRetries"], out var r) ? r : 1,
            MaxFileSizeBytes = long.TryParse(section["MaxFileSizeBytes"], out var s) ? s : 10 * 1024 * 1024
        };
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("RemoveBg:ApiKey is required.");
        }
    }
}
