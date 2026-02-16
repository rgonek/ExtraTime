namespace ExtraTime.Infrastructure.Configuration;

public sealed class ApiFootballSettings
{
    public const string SectionName = "ExternalData:ApiFootball";

    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public int MaxDailyRequests { get; set; } = 100;
    public bool SharedQuotaWithLineups { get; set; } = true;
    public int ReservedForLineupRequests { get; set; } = 100;
    public int StaleAfterHours { get; set; } = 24;
}
