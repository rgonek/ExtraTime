using ExtraTime.Domain.Enums;

namespace ExtraTime.Application.Features.Bots.Strategies;

public sealed class BotStrategyFactory
{
    private readonly Dictionary<BotStrategy, IBotBettingStrategy> _strategies;

    public BotStrategyFactory()
    {
        _strategies = new Dictionary<BotStrategy, IBotBettingStrategy>
        {
            { BotStrategy.Random, new RandomStrategy() },
            { BotStrategy.HomeFavorer, new HomeFavorerStrategy() },
            { BotStrategy.UnderdogSupporter, new UnderdogSupporterStrategy() },
            { BotStrategy.DrawPredictor, new DrawPredictorStrategy() },
            { BotStrategy.HighScorer, new HighScorerStrategy() }
        };
    }

    public IBotBettingStrategy GetStrategy(BotStrategy strategy)
    {
        return _strategies.TryGetValue(strategy, out var impl)
            ? impl
            : _strategies[BotStrategy.Random];
    }
}
