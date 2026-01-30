using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Application.Features.Bots.Strategies;

public sealed class HighScorerStrategy : IBotBettingStrategy
{
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.HighScorer;

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        int homeScore = _random.Next(2, 5);
        int awayScore = _random.Next(1, 4);
        return (homeScore, awayScore);
    }
}
