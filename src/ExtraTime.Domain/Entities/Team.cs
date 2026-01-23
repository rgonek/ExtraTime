using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class Team : BaseEntity
{
    public required int ExternalId { get; set; }
    public required string Name { get; set; }
    public required string ShortName { get; set; }
    public string? Tla { get; set; }
    public string? LogoUrl { get; set; }
    public string? ClubColors { get; set; }
    public string? Venue { get; set; }
    public DateTime LastSyncedAt { get; set; }

    public ICollection<CompetitionTeam> CompetitionTeams { get; set; } = [];
    public ICollection<Match> HomeMatches { get; set; } = [];
    public ICollection<Match> AwayMatches { get; set; } = [];
}
