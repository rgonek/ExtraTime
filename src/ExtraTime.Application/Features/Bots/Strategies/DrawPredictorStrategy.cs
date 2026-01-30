using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Application.Features.Bots.Strategies;

public sealed class DrawPredictorStrategy : IBotBettingStrategy
{
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.DrawPredictor;

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        if (_random.NextDouble() < 0.7)
        {
            int score = _random.Next(0, 3);
            return (score, score);
        }
        int winner = _random.Next(1, 3);
        int loser = winner - 1;
        return _random.NextDouble() < 0.5 ? (winner, loser) : (loser, winner);
    }
}
