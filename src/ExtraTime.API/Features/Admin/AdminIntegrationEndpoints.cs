using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ExtraTime.API.Features.Admin;

public static class AdminIntegrationEndpoints
{
    public static RouteGroupBuilder MapAdminIntegrationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/integrations")
            .WithTags("Admin - Integrations")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetAllStatuses)
            .WithName("GetIntegrationStatuses");

        group.MapGet("/{type}", GetStatus)
            .WithName("GetIntegrationStatus");

        group.MapPost("/{type}/disable", DisableIntegration)
            .WithName("DisableIntegration");

        group.MapPost("/{type}/enable", EnableIntegration)
            .WithName("EnableIntegration");

        group.MapPost("/{type}/sync", TriggerSync)
            .WithName("TriggerIntegrationSync");

        group.MapGet("/availability", GetDataAvailability)
            .WithName("GetDataAvailability");

        return group;
    }

    private static async Task<IResult> GetAllStatuses(
        IIntegrationHealthService healthService,
        CancellationToken cancellationToken)
    {
        var statuses = await healthService.GetAllStatusesAsync(cancellationToken);
        return Results.Ok(statuses);
    }

    private static async Task<IResult> GetStatus(
        string type,
        IIntegrationHealthService healthService,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationType>(type, true, out var integrationType))
        {
            return Results.BadRequest(new { error = "Invalid integration type" });
        }

        var status = await healthService.GetStatusAsync(integrationType, cancellationToken);
        return Results.Ok(status);
    }

    private static async Task<IResult> DisableIntegration(
        string type,
        DisableIntegrationRequest request,
        IIntegrationHealthService healthService,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationType>(type, true, out var integrationType))
        {
            return Results.BadRequest(new { error = "Invalid integration type" });
        }

        if (currentUser.UserId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.BadRequest(new { error = "Reason is required" });
        }

        await healthService.DisableIntegrationAsync(
            integrationType,
            request.Reason,
            currentUser.UserId.Value.ToString(),
            cancellationToken);

        return Results.Ok(new { message = $"{type} disabled" });
    }

    private static async Task<IResult> EnableIntegration(
        string type,
        IIntegrationHealthService healthService,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationType>(type, true, out var integrationType))
        {
            return Results.BadRequest(new { error = "Invalid integration type" });
        }

        await healthService.EnableIntegrationAsync(integrationType, cancellationToken);
        return Results.Ok(new { message = $"{type} enabled" });
    }

    private static async Task<IResult> TriggerSync(
        string type,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationType>(type, true, out var integrationType))
        {
            return Results.BadRequest(new { error = "Invalid integration type" });
        }

        switch (integrationType)
        {
            case IntegrationType.Understat:
            {
                var understat = serviceProvider.GetRequiredService<IUnderstatService>();
                await understat.SyncAllLeaguesAsync(cancellationToken);
                break;
            }
            case IntegrationType.FootballDataUk:
            {
                var odds = serviceProvider.GetRequiredService<IOddsDataService>();
                await odds.ImportAllLeaguesAsync(cancellationToken);
                break;
            }
            case IntegrationType.ApiFootball:
            {
                var injuries = serviceProvider.GetRequiredService<IInjuryService>();
                await injuries.SyncInjuriesForUpcomingMatchesAsync(3, cancellationToken);
                break;
            }
            case IntegrationType.FootballDataOrg:
            {
                var football = serviceProvider.GetRequiredService<IFootballSyncService>();
                await football.SyncMatchesAsync(ct: cancellationToken);
                break;
            }
            case IntegrationType.ClubElo:
            {
                var elo = serviceProvider.GetRequiredService<IEloRatingService>();
                await elo.SyncEloRatingsAsync(cancellationToken);
                break;
            }
            case IntegrationType.LineupProvider:
            {
                var lineups = serviceProvider.GetRequiredService<ILineupSyncService>();
                await lineups.SyncLineupsForUpcomingMatchesAsync(TimeSpan.FromHours(24), cancellationToken);
                break;
            }
            case IntegrationType.SuspensionProvider:
            {
                var suspensions = serviceProvider.GetRequiredService<ISuspensionService>();
                await suspensions.SyncSuspensionsForUpcomingMatchesAsync(3, cancellationToken);
                break;
            }
            default:
                return Results.BadRequest(new { error = "Invalid integration type" });
        }

        return Results.Ok(new { message = $"{type} sync triggered" });
    }

    private static async Task<IResult> GetDataAvailability(
        IIntegrationHealthService healthService,
        CancellationToken cancellationToken)
    {
        var availability = await healthService.GetDataAvailabilityAsync(cancellationToken);
        return Results.Ok(availability);
    }
}

public sealed record DisableIntegrationRequest(string Reason);
