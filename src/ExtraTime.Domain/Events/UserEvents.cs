using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Events;

public sealed record UserRegistered(Guid UserId, string Email) : IDomainEvent;

public sealed record UserLoggedIn(Guid UserId) : IDomainEvent;
