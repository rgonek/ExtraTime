namespace ExtraTime.Infrastructure.Configuration;

public sealed class UnderstatSettings
{
    public const string SectionName = "ExternalData:Understat";

    public bool Enabled { get; set; } = true;
    public int SyncHourUtc { get; set; } = 4;
    public string SyncSchedule { get; set; } = "0 4 * * *";
}
