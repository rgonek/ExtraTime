using ExtraTime.Application.Common;
using Mediator;

namespace ExtraTime.Application.Features.Leagues.Commands.LeaveLeague;

public sealed record LeaveLeagueCommand(Guid LeagueId) : IRequest<Result>;
