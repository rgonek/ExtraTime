using ExtraTime.Domain.Entities;

namespace ExtraTime.Application.Features.Bots.Strategies;

public sealed record MatchAnalysis
{
    public required Match Match { get; init; }
    public required TeamFormCache HomeTeamForm { get; init; }
    public required TeamFormCache AwayTeamForm { get; init; }

    public double HomeFormScore { get; init; }
    public double AwayFormScore { get; init; }
    public double HomeAdvantage { get; init; }
    public double ExpectedHomeGoals { get; init; }
    public double ExpectedAwayGoals { get; init; }
    public bool IsHighStakes { get; init; }
    public bool IsLateSeasonMatch { get; init; }

    public int PredictedHomeScore { get; init; }
    public int PredictedAwayScore { get; init; }
}
