namespace ExtraTime.Infrastructure.Configuration;

public sealed class FootballDataUkSettings
{
    public const string SectionName = "ExternalData:FootballDataUk";

    public bool Enabled { get; set; } = true;
    public int SyncHourUtc { get; set; } = 5;
    public string SyncSchedule { get; set; } = "0 5 * * 1";
}
