using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Football.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Football.Queries.GetMatchById;

public sealed record GetMatchByIdQuery(Guid MatchId) : IRequest<Result<MatchDetailDto>>;
