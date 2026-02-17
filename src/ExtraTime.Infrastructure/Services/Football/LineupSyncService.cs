using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ExtraTime.Infrastructure.Services.Football;

public sealed class LineupSyncService(
    IApplicationDbContext context,
    ILineupDataProvider lineupDataProvider,
    ILogger<LineupSyncService> logger) : ILineupSyncService
{
    public async Task<bool> SyncLineupForMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Competition)
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);

        if (match is null)
        {
            logger.LogWarning("Match {MatchId} not found for lineup sync.", matchId);
            return false;
        }

        var request = new MatchLineupRequest(
            match.ExternalId,
            match.HomeTeam.Name,
            match.AwayTeam.Name,
            match.MatchDateUtc,
            match.Competition.Code);

        var lineup = await lineupDataProvider.GetMatchLineupAsync(request, cancellationToken);
        if (lineup is null)
        {
            logger.LogDebug("Lineup data unavailable for match {MatchId}", matchId);
            return false;
        }

        await UpsertLineupAsync(match.Id, match.HomeTeamId, lineup.HomeTeam, cancellationToken);
        await UpsertLineupAsync(match.Id, match.AwayTeamId, lineup.AwayTeam, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> SyncLineupsForUpcomingMatchesAsync(
        TimeSpan lookAhead,
        CancellationToken cancellationToken = default)
    {
        var now = Clock.UtcNow;
        var cutoff = now.Add(lookAhead);

        var matchIds = await context.Matches
            .Where(m => m.MatchDateUtc >= now && m.MatchDateUtc <= cutoff)
            .Where(m => m.Status == MatchStatus.Scheduled || m.Status == MatchStatus.Timed)
            .Where(m => !context.MatchLineups.Any(ml => ml.MatchId == m.Id))
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var syncedCount = 0;
        foreach (var matchId in matchIds)
        {
            if (await SyncLineupForMatchAsync(matchId, cancellationToken))
            {
                syncedCount++;
            }
        }

        return syncedCount;
    }

    private async Task UpsertLineupAsync(
        Guid matchId,
        Guid teamId,
        TeamLineupData data,
        CancellationToken cancellationToken)
    {
        var startingXi = JsonSerializer.Serialize(
            data.StartingXi.Select(p => new LineupPlayer(p.Id, p.Name, p.Position, p.ShirtNumber)));
        var bench = JsonSerializer.Serialize(
            data.Bench.Select(p => new LineupPlayer(p.Id, p.Name, p.Position, p.ShirtNumber)));

        var existing = await context.MatchLineups
            .FirstOrDefaultAsync(ml => ml.MatchId == matchId && ml.TeamId == teamId, cancellationToken);

        if (existing is not null)
        {
            existing.Update(data.Formation, data.CoachName, startingXi, bench, data.CaptainName);
            return;
        }

        var lineup = MatchLineup.Create(
            matchId,
            teamId,
            data.Formation,
            data.CoachName,
            startingXi,
            bench,
            data.CaptainName);
        context.MatchLineups.Add(lineup);
    }
}
