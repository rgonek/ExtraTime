using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class TeamXgSnapshot : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public required Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public required string Season { get; set; }
    public DateTime SnapshotDateUtc { get; set; }

    public double XgPerMatch { get; set; }
    public double XgAgainstPerMatch { get; set; }
    public double XgOverperformance { get; set; }
    public double RecentXgPerMatch { get; set; }
}
