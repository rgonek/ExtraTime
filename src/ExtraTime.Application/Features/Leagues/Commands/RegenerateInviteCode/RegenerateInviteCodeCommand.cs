using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Leagues.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Leagues.Commands.RegenerateInviteCode;

public sealed record RegenerateInviteCodeCommand(Guid LeagueId, DateTime? ExpiresAt) : IRequest<Result<LeagueDto>>;
