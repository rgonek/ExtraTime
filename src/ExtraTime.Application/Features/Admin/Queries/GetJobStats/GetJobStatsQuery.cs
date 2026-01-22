using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Admin.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Admin.Queries.GetJobStats;

public sealed record GetJobStatsQuery : IRequest<Result<JobStatsDto>>;
