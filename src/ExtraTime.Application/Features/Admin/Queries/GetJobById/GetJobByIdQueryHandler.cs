using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Admin.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Admin.Queries.GetJobById;

public sealed class GetJobByIdQueryHandler(
    IApplicationDbContext context) : IRequestHandler<GetJobByIdQuery, Result<JobDto>>
{
    public async ValueTask<Result<JobDto>> Handle(
        GetJobByIdQuery request,
        CancellationToken cancellationToken)
    {
        var job = await context.BackgroundJobs
            .Where(j => j.Id == request.JobId)
            .Select(j => new JobDto(
                j.Id,
                j.JobType,
                j.Status.ToString(),
                j.Payload,
                j.Result,
                j.Error,
                j.RetryCount,
                j.MaxRetries,
                j.CreatedAt,
                j.StartedAt,
                j.CompletedAt,
                j.ScheduledAt,
                j.CreatedByUserId,
                j.CorrelationId))
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return Result<JobDto>.Failure(AdminErrors.JobNotFound);
        }

        return Result<JobDto>.Success(job);
    }
}
