using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Admin.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Admin.Queries.GetJobById;

public sealed record GetJobByIdQuery(Guid JobId) : IRequest<Result<JobDto>>;
