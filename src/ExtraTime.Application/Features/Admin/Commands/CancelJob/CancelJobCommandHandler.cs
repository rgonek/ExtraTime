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

        try
        {
            job.Cancel();
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(AdminErrors.JobCannotBeCancelled);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
