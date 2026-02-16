namespace ExtraTime.Infrastructure.Configuration;

public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public int TokenLimit { get; set; } = 100;
    public int TokensPerPeriod { get; set; } = 10;
    public int ReplenishPeriodSeconds { get; set; } = 1;
    public int QueueLimit { get; set; } = 0;
    public bool Enabled { get; set; } = true;
    public bool AutoReplenishment { get; set; } = true;
}
