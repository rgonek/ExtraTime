using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.Events;

namespace ExtraTime.Domain.Entities;

public sealed class Bot : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string Name { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public BotStrategy Strategy { get; private set; }
    public string? Configuration { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastBetPlacedAt { get; private set; }

    private Bot() { } // Required for EF Core

    public static Bot Create(
        Guid userId,
        string name,
        BotStrategy strategy,
        string? avatarUrl = null,
        string? configuration = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Bot name is required", nameof(name));

        if (name.Length < 2 || name.Length > 50)
            throw new ArgumentException("Bot name must be between 2 and 50 characters", nameof(name));

        var bot = new Bot
        {
            UserId = userId,
            Name = name,
            Strategy = strategy,
            AvatarUrl = avatarUrl,
            Configuration = configuration,
            IsActive = true,
            CreatedAt = Clock.UtcNow
        };

        bot.AddDomainEvent(new BotCreated(bot.Id, userId, name, strategy));
        return bot;
    }

    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        AddDomainEvent(new BotStatusChanged(Id, false, true));
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        AddDomainEvent(new BotStatusChanged(Id, true, false));
    }

    public void RecordBetPlaced()
    {
        LastBetPlacedAt = Clock.UtcNow;
    }

    public void UpdateConfiguration(string? configuration)
    {
        Configuration = configuration;
    }

    public void UpdateStrategy(BotStrategy strategy)
    {
        Strategy = strategy;
    }

    public void UpdateDetails(string name, string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Bot name is required", nameof(name));

        if (name.Length < 2 || name.Length > 50)
            throw new ArgumentException("Bot name must be between 2 and 50 characters", nameof(name));

        Name = name;
        AvatarUrl = avatarUrl;
    }
}
