using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Bets.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Bets.Queries.GetLeagueStandings;

public sealed record GetLeagueStandingsQuery(Guid LeagueId) : IRequest<Result<List<LeagueStandingDto>>>;
