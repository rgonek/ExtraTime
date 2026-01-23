using ExtraTime.Application.Common;
using Mediator;

namespace ExtraTime.Application.Features.Leagues.Commands.DeleteLeague;

public sealed record DeleteLeagueCommand(Guid LeagueId) : IRequest<Result>;
