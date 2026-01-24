using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Bets.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Bets.Queries.GetMyBets;

public sealed record GetMyBetsQuery(Guid LeagueId) : IRequest<Result<List<MyBetDto>>>;
