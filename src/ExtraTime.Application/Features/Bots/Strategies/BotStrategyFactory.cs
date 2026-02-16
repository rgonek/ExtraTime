using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ExtraTime.Application.Features.Bots.Strategies;

public sealed class BotStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<BotStrategy, Func<IBotBettingStrategy>> _factories;

    public BotStrategyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _factories = new Dictionary<BotStrategy, Func<IBotBettingStrategy>>
        {
            { BotStrategy.Random, () => new RandomStrategy() },
            { BotStrategy.HomeFavorer, () => new HomeFavorerStrategy() },
            { BotStrategy.UnderdogSupporter, () => new UnderdogSupporterStrategy() },
            { BotStrategy.DrawPredictor, () => new DrawPredictorStrategy() },
            { BotStrategy.HighScorer, () => new HighScorerStrategy() },
            { BotStrategy.StatsAnalyst, () => new StatsAnalystStrategy(
                _serviceProvider.GetRequiredService<ITeamFormCalculator>(),
                integrationHealthService: _serviceProvider.GetService<IIntegrationHealthService>(),
                understatService: _serviceProvider.GetService<IUnderstatService>(),
                oddsService: _serviceProvider.GetService<IOddsDataService>(),
                injuryService: _serviceProvider.GetService<IInjuryService>(),
                eloService: _serviceProvider.GetService<IEloRatingService>(),
                logger: _serviceProvider.GetService<ILogger<StatsAnalystStrategy>>()) }
        };
    }

    public IBotBettingStrategy GetStrategy(BotStrategy strategy)
    {
        return _factories.TryGetValue(strategy, out var factory)
            ? factory()
            : _factories[BotStrategy.Random]();
    }
}
