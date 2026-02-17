using ExtraTime.Application.Features.Bots.Queries.GetBots;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bots.Handlers;

public sealed class GetBotsQueryHandlerTests : HandlerTestBase
{
    private readonly GetBotsQueryHandler _handler;

    public GetBotsQueryHandlerTests()
    {
        _handler = new GetBotsQueryHandler(Context);
    }

    [Test]
    public async Task Handle_NoBots_ReturnsEmptyList()
    {
        SetupDbSets([], [], []);

        var result = await _handler.Handle(new GetBotsQuery(), CancellationToken);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEmpty();
    }

    [Test]
    public async Task Handle_IncludeInactiveFalse_ReturnsOnlyActiveBots()
    {
        var bots = new List<Bot>
        {
            new BotBuilder().WithName("ActiveBot").WithIsActive(true).Build(),
            new BotBuilder().WithName("InactiveBot").WithIsActive(false).Build()
        };

        SetupDbSets(bots, [], []);

        var result = await _handler.Handle(new GetBotsQuery(), CancellationToken);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).HasSingleItem();
        await Assert.That(result.Value![0].Name).IsEqualTo("ActiveBot");
    }

    [Test]
    public async Task Handle_StrategyFilter_ReturnsOnlyMatchingBots()
    {
        var bots = new List<Bot>
        {
            new BotBuilder().WithName("RandomBot").WithStrategy(BotStrategy.Random).Build(),
            new BotBuilder().WithName("StatsBot").WithStrategy(BotStrategy.StatsAnalyst).Build(),
            new BotBuilder().WithName("HomeBot").WithStrategy(BotStrategy.HomeFavorer).Build()
        };

        SetupDbSets(bots, [], []);

        var result = await _handler.Handle(
            new GetBotsQuery(IncludeInactive: true, Strategy: BotStrategy.StatsAnalyst),
            CancellationToken);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).HasSingleItem();
        await Assert.That(result.Value![0].Name).IsEqualTo("StatsBot");
        await Assert.That(result.Value[0].Strategy).IsEqualTo("StatsAnalyst");
    }

    [Test]
    public async Task Handle_WithBetData_ReturnsCalculatedStats()
    {
        var userId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        var bot = new BotBuilder()
            .WithId(botId)
            .WithUserId(userId)
            .WithName("StatsBot")
            .Build();

        var betWithResult = new BetBuilder()
            .WithUserId(userId)
            .Build();
        var resultEntity = BetResult.Create(betWithResult.Id, 3, true, true);
        typeof(Bet).GetProperty(nameof(Bet.Result))?.SetValue(betWithResult, resultEntity);

        var betWithoutResult = new BetBuilder()
            .WithUserId(userId)
            .Build();

        var leagueMembership = new LeagueBotMemberBuilder()
            .WithBotId(botId)
            .Build();

        SetupDbSets([bot], [betWithResult, betWithoutResult], [leagueMembership]);

        var result = await _handler.Handle(
            new GetBotsQuery(IncludeInactive: true),
            CancellationToken);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).HasSingleItem();

        var stats = result.Value![0].Stats;
        await Assert.That(stats).IsNotNull();
        await Assert.That(stats!.TotalBetsPlaced).IsEqualTo(2);
        await Assert.That(stats.LeaguesJoined).IsEqualTo(1);
        await Assert.That(stats.AveragePointsPerBet).IsEqualTo(3);
        await Assert.That(stats.ExactPredictions).IsEqualTo(1);
        await Assert.That(stats.CorrectResults).IsEqualTo(1);
    }

    private void SetupDbSets(
        List<Bot> bots,
        List<Bet> bets,
        List<LeagueBotMember> leagueBotMembers)
    {
        var botSet = CreateMockDbSet(bots.AsQueryable());
        var betSet = CreateMockDbSet(bets.AsQueryable());
        var leagueBotMemberSet = CreateMockDbSet(leagueBotMembers.AsQueryable());

        Context.Bots.Returns(botSet);
        Context.Bets.Returns(betSet);
        Context.LeagueBotMembers.Returns(leagueBotMemberSet);
    }
}
