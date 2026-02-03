using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Services.BotStrategies;

public sealed class BotStrategyFactoryTests
{
    private ITeamFormCalculator _formCalculator = null!;

    [Before(Test)]
    public void Setup()
    {
        _formCalculator = Substitute.For<ITeamFormCalculator>();
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_formCalculator);
        return services.BuildServiceProvider();
    }

    [Test]
    public async Task GetStrategy_RandomStrategy_ReturnsRandomStrategyInstance()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var factory = new BotStrategyFactory(serviceProvider);

        // Act
        var strategy = factory.GetStrategy(BotStrategy.Random);

        // Assert
        await Assert.That(strategy).IsTypeOf<RandomStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.Random);
    }

    [Test]
    public async Task GetStrategy_HomeFavorerStrategy_ReturnsHomeFavorerInstance()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var factory = new BotStrategyFactory(serviceProvider);

        // Act
        var strategy = factory.GetStrategy(BotStrategy.HomeFavorer);

        // Assert
        await Assert.That(strategy).IsTypeOf<HomeFavorerStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.HomeFavorer);
    }

    [Test]
    public async Task GetStrategy_DrawPredictorStrategy_ReturnsDrawPredictorInstance()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var factory = new BotStrategyFactory(serviceProvider);

        // Act
        var strategy = factory.GetStrategy(BotStrategy.DrawPredictor);

        // Assert
        await Assert.That(strategy).IsTypeOf<DrawPredictorStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.DrawPredictor);
    }

    [Test]
    public async Task GetStrategy_UnderdogSupporterStrategy_ReturnsUnderdogSupporterInstance()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var factory = new BotStrategyFactory(serviceProvider);

        // Act
        var strategy = factory.GetStrategy(BotStrategy.UnderdogSupporter);

        // Assert
        await Assert.That(strategy).IsTypeOf<UnderdogSupporterStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.UnderdogSupporter);
    }

    [Test]
    public async Task GetStrategy_HighScorerStrategy_ReturnsHighScorerInstance()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var factory = new BotStrategyFactory(serviceProvider);

        // Act
        var strategy = factory.GetStrategy(BotStrategy.HighScorer);

        // Assert
        await Assert.That(strategy).IsTypeOf<HighScorerStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.HighScorer);
    }

    [Test]
    public async Task GetStrategy_StatsAnalystStrategy_ReturnsStatsAnalystInstance()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var factory = new BotStrategyFactory(serviceProvider);

        // Act
        var strategy = factory.GetStrategy(BotStrategy.StatsAnalyst);

        // Assert
        await Assert.That(strategy).IsTypeOf<StatsAnalystStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.StatsAnalyst);
    }

    [Test]
    public async Task GetStrategy_AllStrategies_ReturnsCorrectTypes()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var factory = new BotStrategyFactory(serviceProvider);
        var allStrategies = Enum.GetValues<BotStrategy>();

        // Act & Assert
        foreach (var strategyType in allStrategies)
        {
            var strategy = factory.GetStrategy(strategyType);
            await Assert.That(strategy.StrategyType).IsEqualTo(strategyType);
        }
    }

    [Test]
    public async Task GetStrategy_InvalidStrategy_ReturnsRandomStrategy()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var factory = new BotStrategyFactory(serviceProvider);
        var invalidStrategy = (BotStrategy)999;

        // Act
        var strategy = factory.GetStrategy(invalidStrategy);

        // Assert - Should fallback to Random strategy
        await Assert.That(strategy).IsTypeOf<RandomStrategy>();
        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.Random);
    }

    [Test]
    public async Task GetStrategy_StatsAnalyst_ResolvesCalculatorFromDI()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var factory = new BotStrategyFactory(serviceProvider);

        // Act
        factory.GetStrategy(BotStrategy.StatsAnalyst);

        // Assert - Strategy should work without throwing (calculator was resolved)
        await Assert.That(true).IsTrue(); // Just verify no exception thrown
    }

    [Test]
    public async Task GetStrategy_CreatesNewInstance_EachTime()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var factory = new BotStrategyFactory(serviceProvider);

        // Act
        var strategy1 = factory.GetStrategy(BotStrategy.Random);
        var strategy2 = factory.GetStrategy(BotStrategy.Random);

        // Assert - Should be different instances
        await Assert.That(strategy1).IsNotEqualTo(strategy2);
        await Assert.That(strategy1).IsNotSameReferenceAs(strategy2);
    }

    [Test]
    public async Task Constructor_RegistersAllStrategyFactories()
    {
        // Arrange - Create a new factory with fresh DI
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<ITeamFormCalculator>());
        var serviceProvider = services.BuildServiceProvider();

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
