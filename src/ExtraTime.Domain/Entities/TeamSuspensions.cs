using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class TeamSuspensions : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public int TotalSuspended { get; set; }
    public int KeyPlayersSuspended { get; set; }
    public int CardSuspensions { get; set; }
    public int DisciplinarySuspensions { get; set; }

    public string SuspendedPlayerNames { get; set; } = string.Empty;
    public double SuspensionImpactScore { get; set; }

    public DateTime LastSyncedAt { get; set; }
    public DateTime? NextSyncDue { get; set; }
}
