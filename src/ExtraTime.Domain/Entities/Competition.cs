using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class Competition : BaseEntity
{
    public required int ExternalId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required string Country { get; set; }
    public string? LogoUrl { get; set; }
    public int? CurrentMatchday { get; set; }
    public DateTime? CurrentSeasonStart { get; set; }
    public DateTime? CurrentSeasonEnd { get; set; }
    public DateTime LastSyncedAt { get; set; }

    public ICollection<CompetitionTeam> CompetitionTeams { get; set; } = [];
    public ICollection<Match> Matches { get; set; } = [];
}
