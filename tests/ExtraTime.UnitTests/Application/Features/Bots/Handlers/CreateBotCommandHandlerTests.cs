using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Commands.CreateBot;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bots.Handlers;

public sealed class CreateBotCommandHandlerTests : HandlerTestBase
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly CreateBotCommandHandler _handler;

    public CreateBotCommandHandlerTests()
    {
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _handler = new CreateBotCommandHandler(Context, _passwordHasher);
    }

    [Test]
    public async Task Handle_ValidCommand_CreatesBotAndUser()
    {
        // Arrange
        var command = new CreateBotCommand(
            "TestBot",
            "https://example.com/avatar.png",
            BotStrategy.Random,
            null);

        var mockBots = CreateMockDbSet(new List<Bot>().AsQueryable());
        Context.Bots.Returns(mockBots);

        var mockUsers = CreateMockDbSet(new List<User>().AsQueryable());
        Context.Users.Returns(mockUsers);

        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed_password");

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Name).IsEqualTo("TestBot");
        await Assert.That(result.Value.Strategy).IsEqualTo("Random");
        await Assert.That(result.Value.IsActive).IsTrue();

        Context.Users.Received(1).Add(Arg.Any<User>());
        Context.Bots.Received(1).Add(Arg.Any<Bot>());
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task Handle_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var existingBot = new BotBuilder()
            .WithName("TestBot")
            .Build();

        var mockBots = CreateMockDbSet(new List<Bot> { existingBot }.AsQueryable());
        Context.Bots.Returns(mockBots);

        var command = new CreateBotCommand(
            "TestBot",
            null,
            BotStrategy.Random,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("A bot with this name already exists");
    }

    [Test]
    public async Task Handle_DifferentStrategies_CreatesBotWithCorrectStrategy()
    {
        // Arrange
        var strategies = new[]
        {
            BotStrategy.Random,
            BotStrategy.HomeFavorer,
            BotStrategy.UnderdogSupporter,
            BotStrategy.DrawPredictor,
            BotStrategy.HighScorer,
            BotStrategy.StatsAnalyst
        };

        foreach (var strategy in strategies)
        {
            var mockBots = CreateMockDbSet(new List<Bot>().AsQueryable());
            Context.Bots.Returns(mockBots);

            var mockUsers = CreateMockDbSet(new List<User>().AsQueryable());
            Context.Users.Returns(mockUsers);

            _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed_password");

            var command = new CreateBotCommand(
                $"Bot_{strategy}",
                null,
                strategy,
                null);

            // Act
            var result = await _handler.Handle(command, CancellationToken);

            // Assert
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Value!.Strategy).IsEqualTo(strategy.ToString());
        }
    }

    [Test]
    public async Task Handle_WithConfiguration_CreatesBotWithConfiguration()
    {
        // Arrange
        var command = new CreateBotCommand(
            "StatsBot",
            null,
            BotStrategy.StatsAnalyst,
            "{\"formWeight\": 0.5}");

        var mockBots = CreateMockDbSet(new List<Bot>().AsQueryable());
        Context.Bots.Returns(mockBots);

        var mockUsers = CreateMockDbSet(new List<User>().AsQueryable());
        Context.Users.Returns(mockUsers);

        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed_password");

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        Context.Bots.Received(1).Add(Arg.Is<Bot>(b =>
            b.Name == "StatsBot" &&
            b.Strategy == BotStrategy.StatsAnalyst &&
            b.Configuration == "{\"formWeight\": 0.5}"));
    }
}
