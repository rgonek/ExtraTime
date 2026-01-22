using ExtraTime.Application.Features.Admin;
using ExtraTime.Application.Features.Admin.Commands.CancelJob;
using ExtraTime.Application.Features.Admin.Commands.RetryJob;
using ExtraTime.Application.Features.Admin.Queries.GetJobById;
using ExtraTime.Application.Features.Admin.Queries.GetJobs;
using ExtraTime.Application.Features.Admin.Queries.GetJobStats;
using ExtraTime.Domain.Enums;
using Mediator;

namespace ExtraTime.API.Features.Admin;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/jobs", GetJobsAsync)
            .WithName("GetJobs");

        group.MapGet("/jobs/stats", GetJobStatsAsync)
            .WithName("GetJobStats");

        group.MapGet("/jobs/{id:guid}", GetJobByIdAsync)
            .WithName("GetJobById");

        group.MapPost("/jobs/{id:guid}/retry", RetryJobAsync)
            .WithName("RetryJob");

        group.MapPost("/jobs/{id:guid}/cancel", CancelJobAsync)
            .WithName("CancelJob");
    }

    private static async Task<IResult> GetJobsAsync(
        IMediator mediator,
        int page = 1,
        int pageSize = 20,
        JobStatus? status = null,
        string? jobType = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetJobsQuery(page, pageSize, status, jobType);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetJobStatsAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetJobStatsQuery();
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetJobByIdAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetJobByIdQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == AdminErrors.JobNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> RetryJobAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new RetryJobCommand(id);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == AdminErrors.JobNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(new { message = "Job queued for retry" });
    }

    private static async Task<IResult> CancelJobAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CancelJobCommand(id);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == AdminErrors.JobNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(new { message = "Job cancelled" });
    }
}
