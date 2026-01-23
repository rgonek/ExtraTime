using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Leagues.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Leagues.Queries.GetLeague;

public sealed record GetLeagueQuery(Guid LeagueId) : IRequest<Result<LeagueDetailDto>>;
