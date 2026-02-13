using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExtraTime.Functions.Activities;

public sealed class GetCompetitionsWithoutSeasonActivity(
    IApplicationDbContext context,
    IOptions<FootballDataSettings> settings)
{
    [Function(nameof(GetCompetitionsWithoutSeasonActivity))]
    public async Task<List<int>> Run(
        [ActivityTrigger] object? _,
        CancellationToken ct)
    {
        var supportedIds = settings.Value.SupportedCompetitionIds;

        // Find competitions that exist but have no current season
        var competitionsWithoutSeason = await context.Competitions
            .Where(c => supportedIds.Contains(c.ExternalId))
            .Where(c => !context.Seasons.Any(s => s.CompetitionId == c.Id && s.IsCurrent))
            .Select(c => c.ExternalId)
            .ToListAsync(ct);

        return competitionsWithoutSeason;
    }
}
