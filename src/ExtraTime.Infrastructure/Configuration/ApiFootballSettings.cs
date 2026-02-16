namespace ExtraTime.Infrastructure.Configuration;

public sealed class ApiFootballSettings
{
    public const string SectionName = "ExternalData:ApiFootball";

    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public bool EnableEplOnlyInjurySync { get; set; } = true;
    public bool EnableFplInjuryStatusProvider { get; set; } = true;
    public ExternalDataQuotaPolicy QuotaPolicy { get; set; } = new();
    public LineupQualityGatePolicy LineupQualityGate { get; set; } = new();
    public int StaleAfterHours { get; set; } = 24;
}
