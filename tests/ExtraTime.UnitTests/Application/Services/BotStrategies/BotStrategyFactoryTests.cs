using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Services.BotStrategies;

public sealed class BotStrategyFactoryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITeamFormCalculator _formCalculator;
    private readonly BotStrategyFactory _factory;

    public BotStrategyFactoryTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _formCalculator = Substitute.For<ITeamFormCalculator>();
        
        // Setup the service provider to return the form calculator when requested
        _serviceProvider.GetService(typeof(ITeamFormCalculator)).Returns(_formCalculator);
        _serviceProvider.GetRequiredService(typeof(ITeamFormCalculator)).Returns(_formCalculator);
        
        _factory = new BotStrategyFactory(_serviceProvider);
    }

    [Test]
    public async Task GetStrategy_RandomStrategy_ReturnsRandomStrategyInstance()
    {
        // Act
        var strategy = _factory.GetStrategy(BotStrategy.Random);

        // Assert
        await Assert.That(strategy).IsTypeOf<RandomStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.Random);
    }

    [Test]
    public async Task GetStrategy_HomeFavorerStrategy_ReturnsHomeFavorerInstance()
    {
        // Act
        var strategy = _factory.GetStrategy(BotStrategy.HomeFavorer);

        // Assert
        await Assert.That(strategy).IsTypeOf<HomeFavorerStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.HomeFavorer);
    }

    [Test]
    public async Task GetStrategy_DrawPredictorStrategy_ReturnsDrawPredictorInstance()
    {
        // Act
        var strategy = _factory.GetStrategy(BotStrategy.DrawPredictor);

        // Assert
        await Assert.That(strategy).IsTypeOf<DrawPredictorStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.DrawPredictor);
    }

    [Test]
    public async Task GetStrategy_UnderdogSupporterStrategy_ReturnsUnderdogSupporterInstance()
    {
        // Act
        var strategy = _factory.GetStrategy(BotStrategy.UnderdogSupporter);

        // Assert
        await Assert.That(strategy).IsTypeOf<UnderdogSupporterStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.UnderdogSupporter);
    }

    [Test]
    public async Task GetStrategy_HighScorerStrategy_ReturnsHighScorerInstance()
    {
        // Act
        var strategy = _factory.GetStrategy(BotStrategy.HighScorer);

        // Assert
        await Assert.That(strategy).IsTypeOf<HighScorerStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.HighScorer);
    }

    [Test]
    public async Task GetStrategy_StatsAnalystStrategy_ReturnsStatsAnalystInstance()
    {
        // Act
        var strategy = _factory.GetStrategy(BotStrategy.StatsAnalyst);

        // Assert
        await Assert.That(strategy).IsTypeOf<StatsAnalystStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.StatsAnalyst);
    }

    [Test]
    public async Task GetStrategy_AllStrategies_ReturnsCorrectTypes()
    {
        // Arrange
        var allStrategies = Enum.GetValues<BotStrategy>();

        // Act & Assert
        foreach (var strategyType in allStrategies)
        {
            var strategy = _factory.GetStrategy(strategyType);
            await Assert.That(strategy.StrategyType).IsEqualTo(strategyType);
        }
    }

    [Test]
    public async Task GetStrategy_InvalidStrategy_ReturnsRandomStrategy()
    {
        // Arrange - Use an invalid enum value
        var invalidStrategy = (BotStrategy)999;

        // Act
        var strategy = _factory.GetStrategy(invalidStrategy);

        // Assert - Should fallback to Random strategy
        await Assert.That(strategy).IsTypeOf<RandomStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.Random);
    }

    [Test]
    public async Task GetStrategy_StatsAnalyst_ResolvesCalculatorFromDI()
    {
        // Act
        _factory.GetStrategy(BotStrategy.StatsAnalyst);

        // Assert - The factory should have requested the calculator from DI
        _serviceProvider.Received().GetRequiredService(typeof(ITeamFormCalculator));
    }

    [Test]
    public async Task GetStrategy_CreatesNewInstance_EachTime()
    {
        // Act
        var strategy1 = _factory.GetStrategy(BotStrategy.Random);
        var strategy2 = _factory.GetStrategy(BotStrategy.Random);

        // Assert - Should be different instances
        await Assert.That(strategy1).IsNotEqualTo(strategy2);
        await Assert.That(strategy1).IsNotSameReferenceAs(strategy2);
    }

    [Test]
    public async Task Constructor_RegistersAllStrategyFactories()
    {
        // Arrange - Create a new factory
        var serviceProvider = Substitute.For<IServiceProvider>();
        var formCalculator = Substitute.For<ITeamFormCalculator>();
        serviceProvider.GetRequiredService(typeof(ITeamFormCalculator)).Returns(formCalculator);

        // Act
        var factory = new BotStrategyFactory(serviceProvider);

        // Assert - Should be able to get all strategies without throwing
        foreach (var strategyType in Enum.GetValues<BotStrategy>())
        {
            var strategy = factory.GetStrategy(strategyType);
            await Assert.That(strategy).IsNotNull();
        }
    }
}
