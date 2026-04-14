namespace SecondHandShop.Infrastructure.Services.Analytics;

public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    public int AttributionWindowDays { get; init; } = 30;

    public void Validate()
    {
        if (AttributionWindowDays <= 0)
        {
            throw new InvalidOperationException(
                $"{SectionName}:AttributionWindowDays must be greater than 0.");
        }
    }
}
