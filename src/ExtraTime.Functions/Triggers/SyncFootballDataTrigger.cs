using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Triggers;

public sealed class SyncFootballDataTrigger(ILogger<SyncFootballDataTrigger> logger)
{
    [Function("SyncFootballDataTrigger")]
    public async Task Run(
        [TimerTrigger("0 0 * * * *")] TimerInfo timerInfo,
        [DurableClient] DurableTaskClient client,
        CancellationToken ct)
    {
        logger.LogInformation("Starting football data sync at: {Time}", DateTime.UtcNow);

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(Orchestrators.SyncFootballDataOrchestrator),
            ct);

        logger.LogInformation("Started orchestration: {InstanceId}", instanceId);
    }
}
