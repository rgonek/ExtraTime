using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class Season : BaseEntity
{
    public int ExternalId { get; private set; }
    public Guid CompetitionId { get; private set; }
    public int StartYear { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int CurrentMatchday { get; private set; }
    public Guid? WinnerTeamId { get; private set; }
    public bool IsCurrent { get; private set; }
    public DateTime? TeamsLastSyncedAt { get; private set; }
    public DateTime? StandingsLastSyncedAt { get; private set; }

    public Competition Competition { get; private set; } = null!;
    public Team? Winner { get; private set; }
    public ICollection<SeasonTeam> SeasonTeams { get; private set; } = [];
    public ICollection<Match> Matches { get; private set; } = [];
    public ICollection<FootballStanding> Standings { get; private set; } = [];

    private Season() { }

    public static Season Create(
        int externalId,
        Guid competitionId,
        int startYear,
        DateTime startDate,
        DateTime endDate,
        int currentMatchday,
        bool isCurrent = true)
    {
        if (externalId < 0)
            throw new ArgumentException("External ID must be non-negative", nameof(externalId));

        if (startYear <= 0)
            throw new ArgumentException("Start year must be positive", nameof(startYear));

        return new Season
        {
            ExternalId = externalId,
            CompetitionId = competitionId,
            StartYear = startYear,
            StartDate = startDate,
            EndDate = endDate,
            CurrentMatchday = currentMatchday,
            IsCurrent = isCurrent
        };
    }

    public void UpdateMatchday(int matchday)
    {
        if (matchday <= 0)
            throw new ArgumentException("Matchday must be positive", nameof(matchday));

        CurrentMatchday = matchday;
    }

    public void UpdateDates(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public void SetAsNotCurrent() => IsCurrent = false;

    public void SetWinner(Guid teamId) => WinnerTeamId = teamId;

    public void RecordTeamsSync() => TeamsLastSyncedAt = Clock.UtcNow;

    public void RecordStandingsSync() => StandingsLastSyncedAt = Clock.UtcNow;
}
