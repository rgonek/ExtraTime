using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Entities;

public sealed class FootballStanding : BaseEntity
{
    public Guid SeasonId { get; private set; }
    public Guid TeamId { get; private set; }
    public StandingType Type { get; private set; }
    public string? Stage { get; private set; }
    public string? Group { get; private set; }
    public int Position { get; private set; }
    public int PlayedGames { get; private set; }
    public int Won { get; private set; }
    public int Draw { get; private set; }
    public int Lost { get; private set; }
    public int GoalsFor { get; private set; }
    public int GoalsAgainst { get; private set; }
    public int GoalDifference { get; private set; }
    public int Points { get; private set; }
    public string? Form { get; private set; }

    public Season Season { get; private set; } = null!;
    public Team Team { get; private set; } = null!;

    private FootballStanding() { }

    public static FootballStanding Create(
        Guid seasonId,
        Guid teamId,
        StandingType type,
        string? stage,
        string? group,
        int position,
        int playedGames,
        int won,
        int draw,
        int lost,
        int goalsFor,
        int goalsAgainst,
        int goalDifference,
        int points,
        string? form)
    {
        return new FootballStanding
        {
            SeasonId = seasonId,
            TeamId = teamId,
            Type = type,
            Stage = stage,
            Group = group,
            Position = position,
            PlayedGames = playedGames,
            Won = won,
            Draw = draw,
            Lost = lost,
            GoalsFor = goalsFor,
            GoalsAgainst = goalsAgainst,
            GoalDifference = goalDifference,
            Points = points,
            Form = form
        };
    }

    public void Update(
        int position,
        int playedGames,
        int won,
        int draw,
        int lost,
        int goalsFor,
        int goalsAgainst,
        int goalDifference,
        int points,
        string? form)
    {
        Position = position;
        PlayedGames = playedGames;
        Won = won;
        Draw = draw;
        Lost = lost;
        GoalsFor = goalsFor;
        GoalsAgainst = goalsAgainst;
        GoalDifference = goalDifference;
        Points = points;
        Form = form;
    }
}
