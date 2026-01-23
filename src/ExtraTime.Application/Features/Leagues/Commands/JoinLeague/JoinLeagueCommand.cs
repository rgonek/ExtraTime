using ExtraTime.Application.Common;
using Mediator;

namespace ExtraTime.Application.Features.Leagues.Commands.JoinLeague;

public sealed record JoinLeagueCommand(Guid LeagueId, string InviteCode) : IRequest<Result>;
