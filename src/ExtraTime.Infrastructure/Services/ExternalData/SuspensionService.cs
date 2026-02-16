using System.Text.Json;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services.ExternalData;

public sealed class SuspensionService(
    IApplicationDbContext context,
    IInjuryService injuryService,
    IIntegrationHealthService integrationHealthService,
    ILogger<SuspensionService> logger) : ISuspensionService
{
    public async Task SyncSuspensionsForUpcomingMatchesAsync(
        int daysAhead = 3,
        CancellationToken cancellationToken = default)
    {
        if (daysAhead < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(daysAhead), "Days ahead must be at least 1.");
        }

        var startedAt = DateTime.UtcNow;
        await injuryService.SyncInjuriesForUpcomingMatchesAsync(daysAhead, cancellationToken);

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(daysAhead);
        var matchesQuery = context.Matches
            .Where(m => m.MatchDateUtc >= now && m.MatchDateUtc <= cutoff)
            .Where(m => m.Status == MatchStatus.Scheduled || m.Status == MatchStatus.Timed)
            .Select(m => new { m.HomeTeamId, m.AwayTeamId });

        var homeTeamIds = matchesQuery.Select(m => m.HomeTeamId);
        var awayTeamIds = matchesQuery.Select(m => m.AwayTeamId);
        var teamIds = await homeTeamIds
            .Union(awayTeamIds)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (teamIds.Count == 0)
        {
            logger.LogDebug("No upcoming teams found for suspension sync.");
            await integrationHealthService.RecordSuccessAsync(
                IntegrationType.SuspensionProvider,
                DateTime.UtcNow - startedAt,
                cancellationToken);
            return;
        }

        foreach (var teamId in teamIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SyncTeamSuspensionsAsync(teamId, now, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        await integrationHealthService.RecordSuccessAsync(
            IntegrationType.SuspensionProvider,
            DateTime.UtcNow - startedAt,
            cancellationToken);
    }

    public async Task<TeamSuspensions?> GetTeamSuspensionsAsOfAsync(
        Guid teamId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        return await context.TeamSuspensions
            .AsNoTracking()
            .Where(t => t.TeamId == teamId && t.LastSyncedAt <= asOfUtc)
            .OrderByDescending(t => t.LastSyncedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task SyncTeamSuspensionsAsync(
        Guid teamId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var suspensionInjuries = await context.PlayerInjuries
            .AsNoTracking()
            .Where(i => i.TeamId == teamId && i.IsActive)
            .Where(i => IsSuspensionReason(i.InjuryType))
            .ToListAsync(cancellationToken);

        var existingPlayerSuspensions = await context.PlayerSuspensions
            .Where(s => s.TeamId == teamId)
            .ToListAsync(cancellationToken);
        context.PlayerSuspensions.RemoveRange(existingPlayerSuspensions);

        var mappedPlayerSuspensions = suspensionInjuries
            .Select(injury => new PlayerSuspension
            {
                TeamId = teamId,
                ExternalPlayerId = injury.ExternalPlayerId,
                PlayerName = injury.PlayerName,
                Position = injury.Position,
                IsKeyPlayer = injury.IsKeyPlayer,
                SuspensionReason = injury.InjuryType,
                ExpectedReturn = injury.ExpectedReturn,
                IsActive = injury.IsActive,
                LastUpdatedAt = now
            })
            .ToList();
        context.PlayerSuspensions.AddRange(mappedPlayerSuspensions);

        var teamSuspensions = await context.TeamSuspensions
            .FirstOrDefaultAsync(t => t.TeamId == teamId, cancellationToken);

        if (teamSuspensions is null)
        {
            teamSuspensions = new TeamSuspensions
            {
                TeamId = teamId
            };
            context.TeamSuspensions.Add(teamSuspensions);
        }

        var suspendedNames = mappedPlayerSuspensions
            .Select(x => x.PlayerName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        teamSuspensions.TotalSuspended = mappedPlayerSuspensions.Count;
        teamSuspensions.KeyPlayersSuspended = mappedPlayerSuspensions.Count(x => x.IsKeyPlayer);
        teamSuspensions.CardSuspensions = mappedPlayerSuspensions.Count(x => IsCardSuspension(x.SuspensionReason));
        teamSuspensions.DisciplinarySuspensions = mappedPlayerSuspensions.Count - teamSuspensions.CardSuspensions;
        teamSuspensions.SuspendedPlayerNames = JsonSerializer.Serialize(suspendedNames);
        teamSuspensions.LastSyncedAt = now;
        teamSuspensions.NextSyncDue = now.AddHours(24);
        teamSuspensions.SuspensionImpactScore = CalculateSuspensionImpact(teamSuspensions);
    }

    private static double CalculateSuspensionImpact(TeamSuspensions suspensions)
    {
        var impact = 0d;
        impact += suspensions.TotalSuspended * 6;
        impact += suspensions.KeyPlayersSuspended * 18;
        impact += suspensions.CardSuspensions * 5;
        impact += suspensions.DisciplinarySuspensions * 8;
        return Math.Min(100, impact);
    }

    private static bool IsSuspensionReason(string reason)
    {
        return reason.Contains("suspension", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("suspended", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("card", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("red", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("yellow", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("disciplinary", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCardSuspension(string reason)
    {
        return reason.Contains("card", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("red", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("yellow", StringComparison.OrdinalIgnoreCase);
    }
}
