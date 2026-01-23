using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Football.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Football.Queries.GetCompetitions;

public sealed record GetCompetitionsQuery : IRequest<Result<IReadOnlyList<CompetitionDto>>>;
