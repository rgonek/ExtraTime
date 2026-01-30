using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Application.Features.Bots.Strategies;

public sealed class HomeFavorerStrategy : IBotBettingStrategy
{
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.HomeFavorer;

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        int homeScore = _random.Next(1, 4);
        int awayScore = _random.Next(0, homeScore);
        return (homeScore, awayScore);
    }
}
