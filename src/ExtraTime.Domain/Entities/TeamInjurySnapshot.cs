using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class TeamInjurySnapshot : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public DateTime SnapshotDateUtc { get; set; }
    public int TotalInjured { get; set; }
    public int KeyPlayersInjured { get; set; }
    public double InjuryImpactScore { get; set; }
    public string InjuredPlayerNames { get; set; } = "[]";
}
