using ExtraTime.Application.Common;
using Mediator;

namespace ExtraTime.Application.Features.Leagues.Commands.KickMember;

public sealed record KickMemberCommand(Guid LeagueId, Guid UserId) : IRequest<Result>;
