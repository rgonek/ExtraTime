using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Application.Features.Bots.Strategies;

public sealed class UnderdogSupporterStrategy : IBotBettingStrategy
{
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.UnderdogSupporter;

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        int awayScore = _random.Next(1, 4);
        int homeScore = _random.Next(0, awayScore + 1);
        return (homeScore, awayScore);
    }
}
