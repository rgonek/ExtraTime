using ExtraTime.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services.Football;

public sealed class NullLineupDataProvider(
    ILogger<NullLineupDataProvider> logger) : ILineupDataProvider
{
    public Task<MatchLineupData?> GetMatchLineupAsync(
        MatchLineupRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "NullLineupDataProvider: no provider configured. Match {ExternalId} skipped.",
            request.MatchExternalId);
        return Task.FromResult<MatchLineupData?>(null);
    }
}
