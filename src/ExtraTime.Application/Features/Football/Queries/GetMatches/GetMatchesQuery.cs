using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Football.DTOs;
using ExtraTime.Domain.Enums;
using Mediator;

namespace ExtraTime.Application.Features.Football.Queries.GetMatches;

public sealed record GetMatchesQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? CompetitionId = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    MatchStatus? Status = null) : IRequest<Result<MatchesPagedResponse>>;
