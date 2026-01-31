using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Events;

public sealed record BotCreated(Guid BotId, Guid UserId, string Name, BotStrategy Strategy) : IDomainEvent;

public sealed record BotStatusChanged(Guid BotId, bool WasActive, bool IsNowActive) : IDomainEvent;
