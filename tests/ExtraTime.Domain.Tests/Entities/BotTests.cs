using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.Events;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class BotTests
{
    [Test]
    public async Task Create_WithValidData_CreatesBot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var name = "Test Bot";
        var strategy = BotStrategy.Random;

        // Act
        var bot = Bot.Create(userId, name, strategy);

        // Assert
        await Assert.That(bot.UserId).IsEqualTo(userId);
        await Assert.That(bot.Name).IsEqualTo(name);
        await Assert.That(bot.Strategy).IsEqualTo(strategy);
        await Assert.That(bot.IsActive).IsTrue();
        await Assert.That(bot.Configuration).IsNull();
        await Assert.That(bot.LastBetPlacedAt).IsNull();
        await Assert.That(bot.DomainEvents).Count().IsEqualTo(1);
        await Assert.That(bot.DomainEvents.First()).IsTypeOf<BotCreated>();
    }

    [Test]
    public async Task Create_WithAllParameters_CreatesBot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var name = "Test Bot";
        var strategy = BotStrategy.StatsAnalyst;
        var avatarUrl = "https://example.com/avatar.png";
        var configuration = "{\"weight\": 0.5}";

        // Act
        var bot = Bot.Create(userId, name, strategy, avatarUrl, configuration);

        // Assert
        await Assert.That(bot.AvatarUrl).IsEqualTo(avatarUrl);
        await Assert.That(bot.Configuration).IsEqualTo(configuration);
    }

    [Test]
    public async Task Create_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var name = "";
        var strategy = BotStrategy.Random;

        // Act & Assert
        await Assert.That(() => Bot.Create(userId, name, strategy))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Create_WithNameTooShort_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var name = "A"; // Too short
        var strategy = BotStrategy.Random;

        // Act & Assert
        await Assert.That(() => Bot.Create(userId, name, strategy))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Create_WithNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var name = new string('A', 51); // Too long
        var strategy = BotStrategy.Random;

        // Act & Assert
        await Assert.That(() => Bot.Create(userId, name, strategy))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Activate_SetsIsActiveToTrue()
    {
        // Arrange
        var bot = Bot.Create(Guid.NewGuid(), "Test Bot", BotStrategy.Random);
        bot.Deactivate();
        bot.ClearDomainEvents();

        // Act
        bot.Activate();

        // Assert
        await Assert.That(bot.IsActive).IsTrue();
        await Assert.That(bot.DomainEvents).Count().IsEqualTo(1);
        await Assert.That(bot.DomainEvents.First()).IsTypeOf<BotStatusChanged>();
    }

    [Test]
    public async Task Activate_WhenAlreadyActive_DoesNotRaiseEvent()
    {
        // Arrange
        var bot = Bot.Create(Guid.NewGuid(), "Test Bot", BotStrategy.Random);
        bot.ClearDomainEvents();

        // Act
        bot.Activate();

        // Assert
        await Assert.That(bot.DomainEvents).IsEmpty();
    }

    [Test]
    public async Task Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var bot = Bot.Create(Guid.NewGuid(), "Test Bot", BotStrategy.Random);
        bot.ClearDomainEvents();

        // Act
        bot.Deactivate();

        // Assert
        await Assert.That(bot.IsActive).IsFalse();
        await Assert.That(bot.DomainEvents).Count().IsEqualTo(1);
        await Assert.That(bot.DomainEvents.First()).IsTypeOf<BotStatusChanged>();
    }

    [Test]
    public async Task Deactivate_WhenAlreadyInactive_DoesNotRaiseEvent()
    {
        // Arrange
        var bot = Bot.Create(Guid.NewGuid(), "Test Bot", BotStrategy.Random);
        bot.Deactivate();
        bot.ClearDomainEvents();

        // Act
        bot.Deactivate();

        // Assert
        await Assert.That(bot.DomainEvents).IsEmpty();
    }

    [Test]
    public async Task RecordBetPlaced_UpdatesLastBetPlacedAt()
    {
        // Arrange
        var bot = Bot.Create(Guid.NewGuid(), "Test Bot", BotStrategy.Random);
        await Assert.That(bot.LastBetPlacedAt).IsNull();

        // Act
        bot.RecordBetPlaced();

        // Assert
        await Assert.That(bot.LastBetPlacedAt).IsNotNull();
    }

    [Test]
    public async Task UpdateConfiguration_UpdatesStrategyConfig()
    {
        // Arrange
        var bot = Bot.Create(Guid.NewGuid(), "Test Bot", BotStrategy.StatsAnalyst);
        var newConfig = "{\"weight\": 0.8}";

        // Act
        bot.UpdateConfiguration(newConfig);

        // Assert
        await Assert.That(bot.Configuration).IsEqualTo(newConfig);
    }

    [Test]
    public async Task UpdateConfiguration_WithNull_SetsToNull()
    {
        // Arrange
        var bot = Bot.Create(Guid.NewGuid(), "Test Bot", BotStrategy.StatsAnalyst, configuration: "{\"old\": true}");

        // Act
        bot.UpdateConfiguration(null);

        // Assert
        await Assert.That(bot.Configuration).IsNull();
    }

    [Test]
    public async Task UpdateDetails_UpdatesNameAndAvatar()
    {
        // Arrange
        var bot = Bot.Create(Guid.NewGuid(), "Old Name", BotStrategy.Random);
        var newName = "New Name";
        var newAvatar = "https://example.com/newavatar.png";

        // Act
        bot.UpdateDetails(newName, newAvatar);

        // Assert
        await Assert.That(bot.Name).IsEqualTo(newName);
        await Assert.That(bot.AvatarUrl).IsEqualTo(newAvatar);
    }

    [Test]
    public async Task UpdateDetails_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var bot = Bot.Create(Guid.NewGuid(), "Test Bot", BotStrategy.Random);

        // Act & Assert
        await Assert.That(() => bot.UpdateDetails(""))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task UpdateDetails_WithNameTooShort_ThrowsArgumentException()
    {
        // Arrange
        var bot = Bot.Create(Guid.NewGuid(), "Test Bot", BotStrategy.Random);

        // Act & Assert
        await Assert.That(() => bot.UpdateDetails("A"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task UpdateDetails_WithoutAvatar_SetsAvatarToNull()
    {
        // Arrange
        var bot = Bot.Create(Guid.NewGuid(), "Test Bot", BotStrategy.Random, "https://example.com/avatar.png");

        // Act - when avatarUrl is not provided (null), it sets AvatarUrl to null
        bot.UpdateDetails("New Name");

        // Assert - AvatarUrl is set to null when not provided
        await Assert.That(bot.Name).IsEqualTo("New Name");
        await Assert.That(bot.AvatarUrl).IsNull();
    }

    [Test]
    public async Task Create_WithAllStrategies_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var strategies = Enum.GetValues<BotStrategy>();

        // Act & Assert
        foreach (var strategy in strategies)
        {
            var bot = Bot.Create(userId, $"Bot {strategy}", strategy);
            await Assert.That(bot.Strategy).IsEqualTo(strategy);
        }
    }
}
