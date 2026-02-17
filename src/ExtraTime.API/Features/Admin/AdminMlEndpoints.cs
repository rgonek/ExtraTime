using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.API.Features.Admin;

public static class AdminMlEndpoints
{
    public static RouteGroupBuilder MapAdminMlEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/ml")
            .WithTags("Admin - ML")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/models", GetModelVersionsAsync)
            .WithName("GetMlModelVersions");

        group.MapPost("/models/{version}/activate", ActivateModelVersionAsync)
            .WithName("ActivateMlModelVersion");

        group.MapGet("/accuracy", GetAccuracyComparisonAsync)
            .WithName("GetMlAccuracyComparison");

        group.MapPost("/accuracy/recalculate", RecalculateAccuracyAsync)
            .WithName("RecalculateMlAccuracy");

        group.MapPost("/train", TriggerTrainingAsync)
            .WithName("TriggerMlTraining");

        return group;
    }

    private static async Task<IResult> GetModelVersionsAsync(
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var models = await context.MlModelVersions
            .AsNoTracking()
            .OrderByDescending(model => model.TrainedAt)
            .Select(model => new
            {
                model.Id,
                model.ModelType,
                model.Version,
                model.IsActive,
                model.TrainedAt,
                model.TrainingSamples,
                model.Rsquared,
                model.MeanAbsoluteError,
                model.RootMeanSquaredError,
                model.AlgorithmUsed
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(models);
    }

    private static async Task<IResult> ActivateModelVersionAsync(
        string version,
        ActivateMlModelRequest request,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var targetModels = await context.MlModelVersions
            .Where(model => model.Version == version)
            .ToListAsync(cancellationToken);

        if (targetModels.Count == 0)
        {
            return Results.NotFound(new { error = "Model version not found" });
        }

        var allActiveModels = await context.MlModelVersions
            .Where(model => model.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var activeModel in allActiveModels)
        {
            activeModel.IsActive = false;
            activeModel.ActivatedAt = null;
        }

        foreach (var target in targetModels)
        {
            target.IsActive = true;
            target.ActivatedAt = DateTime.UtcNow;
            target.ActivationNotes = request.Notes;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { message = "Model version activated", version });
    }

    private static async Task<IResult> GetAccuracyComparisonAsync(
        string? period,
        IApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var targetPeriod = string.IsNullOrWhiteSpace(period) ? "monthly" : period;

        var accuracies = await context.BotPredictionAccuracies
            .AsNoTracking()
            .Where(accuracy => accuracy.PeriodType == targetPeriod)
            .OrderByDescending(accuracy => accuracy.PeriodEnd)
            .ToListAsync(cancellationToken);

        var grouped = accuracies
            .GroupBy(accuracy => accuracy.Strategy)
            .Select(group => new
            {
                Strategy = group.Key.ToString(),
                AvgExactAccuracy = group.Average(accuracy => accuracy.ExactScoreAccuracy),
                AvgResultAccuracy = group.Average(accuracy => accuracy.CorrectResultAccuracy),
                AvgMAE = group.Average(accuracy => accuracy.MeanAbsoluteError),
                TotalPredictions = group.Sum(accuracy => accuracy.TotalPredictions),
                LatestPeriod = group.Max(accuracy => accuracy.PeriodEnd)
            });

        return Results.Ok(grouped);
    }

    private static async Task<IResult> RecalculateAccuracyAsync(
        RecalculateMlAccuracyRequest request,
        PredictionAccuracyTracker tracker,
        CancellationToken cancellationToken)
    {
        await tracker.RecalculateAccuracyAsync(
            request.FromDate,
            request.ToDate,
            request.PeriodType,
            cancellationToken);

        return Results.Ok(new
        {
            message = "Prediction accuracy recalculated",
            request.FromDate,
            request.ToDate,
            request.PeriodType
        });
    }

    private static IResult TriggerTrainingAsync(TriggerMlTrainingRequest request)
    {
        return Results.Accepted($"/api/admin/ml/train", new
        {
            message = "ML training request accepted. Use ExtraTime.MLTrainer to execute training.",
            request.League,
            request.FromDate
        });
    }
}

public sealed record ActivateMlModelRequest(string? Notes);

public sealed record RecalculateMlAccuracyRequest(
    DateTime FromDate,
    DateTime ToDate,
    string PeriodType = "custom");

public sealed record TriggerMlTrainingRequest(
    string League,
    DateTime? FromDate = null);
