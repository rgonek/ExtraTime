using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Events;

public sealed record BetPlaced(Guid BetId, Guid UserId, Guid MatchId) : IDomainEvent;

public sealed record BetUpdated(Guid BetId, int NewHomeScore, int NewAwayScore) : IDomainEvent;

public sealed record BetScored(Guid BetId, int PointsEarned) : IDomainEvent;
