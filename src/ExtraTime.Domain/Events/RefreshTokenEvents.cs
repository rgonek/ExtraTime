using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Events;

public sealed record RefreshTokenRevoked(Guid TokenId, Guid UserId, string? Reason) : IDomainEvent;

public sealed record RefreshTokenRotated(Guid OldTokenId, Guid NewTokenId, Guid UserId) : IDomainEvent;
