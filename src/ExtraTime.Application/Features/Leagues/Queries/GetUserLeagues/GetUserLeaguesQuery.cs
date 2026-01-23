using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Leagues.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Leagues.Queries.GetUserLeagues;

public sealed record GetUserLeaguesQuery : IRequest<Result<List<LeagueSummaryDto>>>;
