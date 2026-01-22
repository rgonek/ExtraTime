using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Admin.Commands.RetryJob;

public sealed class RetryJobCommandHandler(
    IApplicationDbContext context,
    IJobDispatcher jobDispatcher) : IRequestHandler<RetryJobCommand, Result>
{
    public async ValueTask<Result> Handle(
        RetryJobCommand request,
        CancellationToken cancellationToken)
    {
        var job = await context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job is null)
        {
            return Result.Failure(AdminErrors.JobNotFound);
        }

        if (job.Status != JobStatus.Failed)
        {
            return Result.Failure(AdminErrors.JobCannotBeRetried);
        }

        if (job.RetryCount >= job.MaxRetries)
        {
            return Result.Failure(AdminErrors.MaxRetriesExceeded);
        }

        job.Status = JobStatus.Pending;
        job.Error = null;
        job.RetryCount++;
        job.StartedAt = null;
        job.CompletedAt = null;

        await context.SaveChangesAsync(cancellationToken);

        await jobDispatcher.DispatchAsync(job, cancellationToken);

        return Result.Success();
    }
}
