using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Application.Features.Bots.Strategies;

public sealed class RandomStrategy : IBotBettingStrategy
{
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.Random;

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        return (_random.Next(0, 5), _random.Next(0, 4));
    }
}
