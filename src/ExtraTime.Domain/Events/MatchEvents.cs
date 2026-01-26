using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Events;

public sealed record MatchStatusChanged(Guid MatchId, MatchStatus OldStatus, MatchStatus NewStatus) : IDomainEvent;

public sealed record MatchScoreUpdated(Guid MatchId, int? HomeScore, int? AwayScore) : IDomainEvent;
