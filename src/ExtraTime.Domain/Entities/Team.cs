using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class Team : BaseEntity
{
    public int ExternalId { get; private set; }
    public string Name { get; private set; } = null!;
    public string ShortName { get; private set; } = null!;
    public string? Tla { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? ClubColors { get; private set; }
    public string? Venue { get; private set; }
    public DateTime LastSyncedAt { get; private set; }

    public ICollection<CompetitionTeam> CompetitionTeams { get; private set; } = [];
    public ICollection<Match> HomeMatches { get; private set; } = [];
    public ICollection<Match> AwayMatches { get; private set; } = [];
    public ICollection<SeasonTeam> SeasonTeams { get; private set; } = [];
    public ICollection<FootballStanding> FootballStandings { get; private set; } = [];

    private Team() { } // Required for EF Core

    public static Team Create(
        int externalId,
        string name,
        string shortName,
        string? tla = null,
        string? logoUrl = null,
        string? clubColors = null,
        string? venue = null)
    {
        if (externalId <= 0)
            throw new ArgumentException("External ID must be positive", nameof(externalId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(shortName))
            throw new ArgumentException("Short name is required", nameof(shortName));

        return new Team
        {
            ExternalId = externalId,
            Name = name,
            ShortName = shortName,
            Tla = tla,
            LogoUrl = logoUrl,
            ClubColors = clubColors,
            Venue = venue,
            LastSyncedAt = Clock.UtcNow
        };
    }

    public void UpdateDetails(
        string name,
        string shortName,
        string? tla = null,
        string? logoUrl = null,
        string? clubColors = null,
        string? venue = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(shortName))
            throw new ArgumentException("Short name is required", nameof(shortName));

        Name = name;
        ShortName = shortName;
        Tla = tla;
        LogoUrl = logoUrl;
        ClubColors = clubColors;
        Venue = venue;
    }

    public void RecordSync()
    {
        LastSyncedAt = Clock.UtcNow;
    }
}
