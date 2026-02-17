using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.ValueObjects;
using ExtraTime.Infrastructure.Services.Bots;
using ExtraTime.UnitTests.Attributes;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

[TestCategory("Significant")]
public sealed class BotSeederTests : HandlerTestBase
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly BotSeeder _seeder;
    private readonly List<Bot> _bots = [];
    private readonly List<User> _users = [];

    public BotSeederTests()
    {
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");

        var botSet = CreateMockDbSet(_bots.AsQueryable());
        botSet.When(set => set.Add(Arg.Any<Bot>()))
            .Do(call => _bots.Add(call.Arg<Bot>()));

        var userSet = CreateMockDbSet(_users.AsQueryable());
        userSet.When(set => set.Add(Arg.Any<User>()))
            .Do(call => _users.Add(call.Arg<User>()));

        Context.Bots.Returns(botSet);
        Context.Users.Returns(userSet);
        Context.SaveChangesAsync(CancellationToken).Returns(1);

        _seeder = new BotSeeder(Context, _passwordHasher);
    }

    [Test]
    public async Task SeedDefaultBotsAsync_WhenNoBotsExist_SeedsAllDefaultBotsIncludingPhase95()
    {
        await _seeder.SeedDefaultBotsAsync(CancellationToken);

        await Assert.That(_bots.Count).IsEqualTo(18);
        await Assert.That(_users.Count).IsEqualTo(18);
        await Assert.That(_bots.All(bot =>
            bot.Strategy == BotStrategy.StatsAnalyst ||
            bot.Strategy == BotStrategy.MachineLearning ||
            bot.Configuration is null)).IsTrue();

        var dataScientist = _bots.Single(bot => bot.Name == "Data Scientist");
        var xgExpert = _bots.Single(bot => bot.Name == "xG Expert");
        var marketFollower = _bots.Single(bot => bot.Name == "Market Follower");
        var injuryTracker = _bots.Single(bot => bot.Name == "Injury Tracker");
        var mlConservative = _bots.Single(bot => bot.Name == "ML Conservative");
        var mlBalanced = _bots.Single(bot => bot.Name == "ML Balanced");
        var mlAggressive = _bots.Single(bot => bot.Name == "ML Aggressive");

        await Assert.That(dataScientist.Strategy).IsEqualTo(BotStrategy.StatsAnalyst);
        await Assert.That(StatsAnalystConfig.FromJson(dataScientist.Configuration)).IsEqualTo(StatsAnalystConfig.FullAnalysis);
        await Assert.That(StatsAnalystConfig.FromJson(xgExpert.Configuration)).IsEqualTo(StatsAnalystConfig.XgFocused);
        await Assert.That(StatsAnalystConfig.FromJson(marketFollower.Configuration)).IsEqualTo(StatsAnalystConfig.MarketFollower);
        await Assert.That(StatsAnalystConfig.FromJson(injuryTracker.Configuration)).IsEqualTo(StatsAnalystConfig.InjuryAware);
        await Assert.That(mlConservative.Strategy).IsEqualTo(BotStrategy.MachineLearning);
        await Assert.That(mlConservative.Configuration).Contains("conservative");
        await Assert.That(mlBalanced.Strategy).IsEqualTo(BotStrategy.MachineLearning);
        await Assert.That(mlBalanced.Configuration).Contains("balanced");
        await Assert.That(mlAggressive.Strategy).IsEqualTo(BotStrategy.MachineLearning);
        await Assert.That(mlAggressive.Configuration).Contains("aggressive");

        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task SeedDefaultBotsAsync_WhenBotsAlreadyExist_DoesNothing()
    {
        _bots.Add(new BotBuilder().WithName("Existing Bot").Build());

        await _seeder.SeedDefaultBotsAsync(CancellationToken);

        await Assert.That(_bots.Count).IsEqualTo(1);
        await Assert.That(_users).IsEmpty();
        await Context.DidNotReceive().SaveChangesAsync(CancellationToken);
    }
}
