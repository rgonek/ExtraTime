using System.Text.Json;
using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class MatchLineup : BaseEntity
{
    public Guid MatchId { get; private set; }
    public Match Match { get; private set; } = null!;

    public Guid TeamId { get; private set; }
    public Team Team { get; private set; } = null!;

    public string? Formation { get; private set; }
    public string? CoachName { get; private set; }
    public string StartingXi { get; private set; } = "[]";
    public string Bench { get; private set; } = "[]";
    public string? CaptainName { get; private set; }
    public DateTime SyncedAt { get; private set; }

    private MatchLineup()
    {
    }

    public static MatchLineup Create(
        Guid matchId,
        Guid teamId,
        string? formation,
        string? coachName,
        string startingXi,
        string bench,
        string? captainName)
    {
        return new MatchLineup
        {
            MatchId = matchId,
            TeamId = teamId,
            Formation = formation,
            CoachName = coachName,
            StartingXi = startingXi,
            Bench = bench,
            CaptainName = captainName,
            SyncedAt = Clock.UtcNow
        };
    }

    public void Update(
        string? formation,
        string? coachName,
        string startingXi,
        string bench,
        string? captainName)
    {
        Formation = formation;
        CoachName = coachName;
        StartingXi = startingXi;
        Bench = bench;
        CaptainName = captainName;
        SyncedAt = Clock.UtcNow;
    }

    public List<LineupPlayer> GetStartingPlayers()
    {
        try
        {
            return JsonSerializer.Deserialize<List<LineupPlayer>>(StartingXi) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public List<LineupPlayer> GetBenchPlayers()
    {
        try
        {
            return JsonSerializer.Deserialize<List<LineupPlayer>>(Bench) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

public sealed record LineupPlayer(
    int Id,
    string Name,
    string? Position,
    int? ShirtNumber);
