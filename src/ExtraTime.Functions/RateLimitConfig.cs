namespace ExtraTime.Functions;

public static class RateLimitConfig
{
    public const int MaxCallsPerMinute = 10;
    public const int CompetitionsPerBatch = 8;
    public static readonly TimeSpan BatchWaitTime = TimeSpan.FromSeconds(65);
}
