using System.Text.Json;
using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class TeamUsualLineup : BaseEntity
{
    public Guid TeamId { get; private set; }
    public Team Team { get; private set; } = null!;

    public Guid SeasonId { get; private set; }
    public Season Season { get; private set; } = null!;

    public string? UsualFormation { get; private set; }
    public string UsualGoalkeepers { get; private set; } = "[]";
    public string UsualDefenders { get; private set; } = "[]";
    public string UsualMidfielders { get; private set; } = "[]";
    public string UsualForwards { get; private set; } = "[]";
    public string? CaptainName { get; private set; }
    public int MatchesAnalyzed { get; private set; }
    public DateTime CalculatedAt { get; private set; }

    private TeamUsualLineup()
    {
    }

    public static TeamUsualLineup Create(
        Guid teamId,
        Guid seasonId,
        string? usualFormation,
        string goalkeepers,
        string defenders,
        string midfielders,
        string forwards,
        string? captainName,
        int matchesAnalyzed)
    {
        return new TeamUsualLineup
        {
            TeamId = teamId,
            SeasonId = seasonId,
            UsualFormation = usualFormation,
            UsualGoalkeepers = goalkeepers,
            UsualDefenders = defenders,
            UsualMidfielders = midfielders,
            UsualForwards = forwards,
            CaptainName = captainName,
            MatchesAnalyzed = matchesAnalyzed,
            CalculatedAt = Clock.UtcNow
        };
    }

    public void Update(
        string? usualFormation,
        string goalkeepers,
        string defenders,
        string midfielders,
        string forwards,
        string? captainName,
        int matchesAnalyzed)
    {
        UsualFormation = usualFormation;
        UsualGoalkeepers = goalkeepers;
        UsualDefenders = defenders;
        UsualMidfielders = midfielders;
        UsualForwards = forwards;
        CaptainName = captainName;
        MatchesAnalyzed = matchesAnalyzed;
        CalculatedAt = Clock.UtcNow;
    }

    public List<UsualPlayer> GetGoalkeepers() => DeserializePlayers(UsualGoalkeepers);
    public List<UsualPlayer> GetDefenders() => DeserializePlayers(UsualDefenders);
    public List<UsualPlayer> GetMidfielders() => DeserializePlayers(UsualMidfielders);
    public List<UsualPlayer> GetForwards() => DeserializePlayers(UsualForwards);

    public List<UsualPlayer> GetAllUsualPlayers() =>
        [.. GetGoalkeepers(), .. GetDefenders(), .. GetMidfielders(), .. GetForwards()];

    private static List<UsualPlayer> DeserializePlayers(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<UsualPlayer>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

public sealed record UsualPlayer(
    int Id,
    string Name,
    string? Position,
    int Appearances);
