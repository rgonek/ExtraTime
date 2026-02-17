namespace ExtraTime.Infrastructure.Configuration;

public sealed class ClubEloSettings
{
    public const string SectionName = "ExternalData:ClubElo";

    public bool Enabled { get; set; } = true;
    public int SyncHourUtc { get; set; } = 3;
    public string SyncSchedule { get; set; } = "0 3 * * *";
}
