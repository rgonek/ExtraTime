using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Triggers;

public sealed class SyncFootballDataTrigger(ILogger<SyncFootballDataTrigger> logger)
{
    private const string SyncInstanceId = "football-data-sync-hourly";

    [Function("SyncFootballDataTrigger")]
    public async Task Run(
        [TimerTrigger("0 0 * * * *")] TimerInfo timerInfo,
        [DurableClient] DurableTaskClient client,
        CancellationToken ct)
    {
        logger.LogInformation("Starting football data sync at: {Time}", DateTime.UtcNow);

        var existingInstance = await client.GetInstanceAsync(SyncInstanceId, ct);
        if (existingInstance is not null && IsOrchestrationActive(existingInstance.RuntimeStatus))
        {
            logger.LogInformation(
                "Skipping football data sync because orchestration {InstanceId} is still {RuntimeStatus}",
                SyncInstanceId,
                existingInstance.RuntimeStatus);
            return;
        }

        var options = new StartOrchestrationOptions
        {
            InstanceId = SyncInstanceId
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(Orchestrators.SyncFootballDataOrchestrator),
            options,
            ct);

        logger.LogInformation("Started orchestration: {InstanceId}", instanceId);
    }

    private static bool IsOrchestrationActive(OrchestrationRuntimeStatus runtimeStatus)
    {
        return runtimeStatus != OrchestrationRuntimeStatus.Completed
               && runtimeStatus != OrchestrationRuntimeStatus.Failed
               && runtimeStatus != OrchestrationRuntimeStatus.Terminated;
    }
}
