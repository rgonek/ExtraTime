using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Events;

public sealed record LeagueCreated(Guid LeagueId, Guid OwnerId) : IDomainEvent;

public sealed record LeagueMemberAdded(Guid LeagueId, Guid UserId) : IDomainEvent;

public sealed record LeagueMemberRemoved(Guid LeagueId, Guid UserId) : IDomainEvent;

public sealed record LeagueInviteCodeRegenerated(Guid LeagueId, string NewCode) : IDomainEvent;
