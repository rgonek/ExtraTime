using System.Collections.Concurrent;
using ExtraTime.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExtraTime.Infrastructure.Services.Football;

public sealed class RateLimitingHandler(
    IOptions<FootballDataSettings> settings,
    ILogger<RateLimitingHandler> logger) : DelegatingHandler
{
    private static readonly ConcurrentQueue<DateTime> RequestTimestamps = new();
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private readonly int _requestsPerMinute = settings.Value.RequestsPerMinute;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            await WaitForRateLimitAsync(cancellationToken);
            RequestTimestamps.Enqueue(DateTime.UtcNow);
        }
        finally
        {
            Semaphore.Release();
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task WaitForRateLimitAsync(CancellationToken cancellationToken)
    {
        CleanupOldTimestamps();

        while (RequestTimestamps.Count >= _requestsPerMinute)
        {
            if (RequestTimestamps.TryPeek(out var oldestRequest))
            {
                var waitTime = oldestRequest.AddMinutes(1) - DateTime.UtcNow;
                if (waitTime > TimeSpan.Zero)
                {
                    logger.LogInformation("Rate limit reached, waiting {WaitTime}...", waitTime);
                    await Task.Delay(waitTime, cancellationToken);
                }
            }
            CleanupOldTimestamps();
        }
    }

    private static void CleanupOldTimestamps()
    {
        var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
        while (RequestTimestamps.TryPeek(out var timestamp) && timestamp < oneMinuteAgo)
        {
            RequestTimestamps.TryDequeue(out _);
        }
    }
}
