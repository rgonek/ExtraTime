using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class Competition : BaseEntity
{
    public int ExternalId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public string? LogoUrl { get; private set; }
    public int? CurrentMatchday { get; private set; }
    public DateTime? CurrentSeasonStart { get; private set; }
    public DateTime? CurrentSeasonEnd { get; private set; }
    public DateTime LastSyncedAt { get; private set; }

    public ICollection<CompetitionTeam> CompetitionTeams { get; private set; } = [];
    public ICollection<Match> Matches { get; private set; } = [];
    public ICollection<Season> Seasons { get; private set; } = [];

    private Competition() { } // Required for EF Core

    public static Competition Create(
        int externalId,
        string name,
        string code,
        string country,
        string? logoUrl = null)
    {
        if (externalId <= 0)
            throw new ArgumentException("External ID must be positive", nameof(externalId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required", nameof(country));

        return new Competition
        {
            ExternalId = externalId,
            Name = name,
            Code = code,
            Country = country,
            LogoUrl = logoUrl,
            LastSyncedAt = Clock.UtcNow
        };
    }

    public void UpdateDetails(
        string name,
        string code,
        string country,
        string? logoUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required", nameof(country));

        Name = name;
        Code = code;
        Country = country;
        LogoUrl = logoUrl;
    }

    public void RecordSync()
    {
        LastSyncedAt = Clock.UtcNow;
    }

    public void UpdateCurrentSeason(int? matchday, DateTime? seasonStart, DateTime? seasonEnd)
    {
        CurrentMatchday = matchday;
        CurrentSeasonStart = seasonStart;
        CurrentSeasonEnd = seasonEnd;
        LastSyncedAt = Clock.UtcNow;
    }
}
