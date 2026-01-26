using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Admin.Commands.CancelJob;

public sealed class CancelJobCommandHandler(
    IApplicationDbContext context) : IRequestHandler<CancelJobCommand, Result>
{
    public async ValueTask<Result> Handle(
        CancelJobCommand request,
        CancellationToken cancellationToken)
    {
        var job = await context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job is null)
        {
            return Result.Failure(AdminErrors.JobNotFound);
        }

        if (job.Status is not (JobStatus.Pending or JobStatus.Processing))
        {
            return Result.Failure(AdminErrors.JobCannotBeCancelled);
        }

        job.Status = JobStatus.Cancelled;
        job.CompletedAt = Clock.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
