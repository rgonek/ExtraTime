using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Entities;

public sealed class Match : BaseEntity
{
    public required int ExternalId { get; set; }

    public Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public Guid HomeTeamId { get; set; }
    public Team HomeTeam { get; set; } = null!;

    public Guid AwayTeamId { get; set; }
    public Team AwayTeam { get; set; } = null!;

    public required DateTime MatchDateUtc { get; set; }
    public required MatchStatus Status { get; set; }
    public int? Matchday { get; set; }
    public string? Stage { get; set; }
    public string? Group { get; set; }

    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public int? HomeHalfTimeScore { get; set; }
    public int? AwayHalfTimeScore { get; set; }

    public string? Venue { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
