using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets.Commands.CalculateBetResults;
using ExtraTime.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services;

public sealed class BetResultsService(
    IApplicationDbContext context,
    IMediator mediator,
    ILogger<BetResultsService> logger) : IBetResultsService
{
    public async Task<int> CalculateAllPendingBetResultsAsync(CancellationToken ct = default)
    {
        var uncalculatedMatches = await context.Bets
            .Include(b => b.Match)
            .Where(b => b.Match.Status == MatchStatus.Finished
                     && b.Match.HomeScore.HasValue
                     && b.Match.AwayScore.HasValue
                     && b.Result == null)
            .Select(b => new { b.Match.Id, b.Match.CompetitionId })
            .Distinct()
            .ToListAsync(ct);

        if (uncalculatedMatches.Count == 0)
        {
            logger.LogInformation("No pending bet results to calculate");
            return 0;
        }

        var processedCount = 0;
        foreach (var match in uncalculatedMatches)
        {
            var command = new CalculateBetResultsCommand(match.Id, match.CompetitionId);
            var result = await mediator.Send(command, ct);
            if (result.IsSuccess)
            {
                processedCount++;
            }
        }

        return processedCount;
    }
}
