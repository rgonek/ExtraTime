using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.Events;

namespace ExtraTime.Domain.Entities;

public sealed class Match : BaseEntity
{
    public int ExternalId { get; private set; }

    public Guid CompetitionId { get; private set; }
    public Competition Competition { get; private set; } = null!;

    public Guid HomeTeamId { get; private set; }
    public Team HomeTeam { get; private set; } = null!;

    public Guid AwayTeamId { get; private set; }
    public Team AwayTeam { get; private set; } = null!;

    public DateTime MatchDateUtc { get; private set; }
    public MatchStatus Status { get; private set; }
    public int? Matchday { get; private set; }
    public string? Stage { get; private set; }
    public string? Group { get; private set; }

    public int? HomeScore { get; private set; }
    public int? AwayScore { get; private set; }
    public int? HomeHalfTimeScore { get; private set; }
    public int? AwayHalfTimeScore { get; private set; }

    public string? Venue { get; private set; }
    public DateTime LastSyncedAt { get; private set; }

    private Match() { } // Required for EF Core

    public static Match Create(
        int externalId,
        Guid competitionId,
        Guid homeTeamId,
        Guid awayTeamId,
        DateTime matchDateUtc,
        MatchStatus status,
        int? matchday = null,
        string? stage = null,
        string? group = null,
        string? venue = null)
    {
        return new Match
        {
            ExternalId = externalId,
            CompetitionId = competitionId,
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            MatchDateUtc = matchDateUtc,
            Status = status,
            Matchday = matchday,
            Stage = stage,
            Group = group,
            Venue = venue,
            LastSyncedAt = Clock.UtcNow
        };
    }

    public void UpdateStatus(MatchStatus newStatus)
    {
        if (Status == newStatus) return;

        // Prevent moving from final states back to active states unless necessary
        if ((Status == MatchStatus.Finished || Status == MatchStatus.Cancelled) &&
            (newStatus == MatchStatus.Scheduled || newStatus == MatchStatus.InPlay))
        {
            // For now, allow it but maybe log it? External API sometimes corrects status.
        }

        var oldStatus = Status;
        Status = newStatus;
        LastSyncedAt = Clock.UtcNow;

        AddDomainEvent(new MatchStatusChanged(Id, oldStatus, newStatus));
    }

    public void UpdateScore(int? home, int? away, int? homeHalf = null, int? awayHalf = null)
    {
        if (home < 0 || away < 0)
            throw new ArgumentException("Scores cannot be negative");

        HomeScore = home;
        AwayScore = away;
        HomeHalfTimeScore = homeHalf;
        AwayHalfTimeScore = awayHalf;
        LastSyncedAt = Clock.UtcNow;

        AddDomainEvent(new MatchScoreUpdated(Id, home, away));
    }

    public void UpdateMetadata(int? matchday, string? stage, string? group, string? venue)
    {
        Matchday = matchday;
        Stage = stage;
        Group = group;
        Venue = venue;
        LastSyncedAt = Clock.UtcNow;
    }

    public bool IsOpenForBetting(int deadlineMinutes, DateTime currentTime)
    {
        // Only scheduled matches can be bet on
        if (Status != MatchStatus.Scheduled && Status != MatchStatus.Timed)
            return false;

        return currentTime <= MatchDateUtc.AddMinutes(-deadlineMinutes);
    }

    public void SyncDetails(
        DateTime matchDateUtc,
        MatchStatus status,
        int? homeScore,
        int? awayScore,
        int? homeHalf = null,
        int? awayHalf = null)
    {
        MatchDateUtc = matchDateUtc;
        UpdateStatus(status);
        UpdateScore(homeScore, awayScore, homeHalf, awayHalf);
        LastSyncedAt = Clock.UtcNow;
    }
}
