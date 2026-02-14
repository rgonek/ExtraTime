using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Functions.Orchestrators;
using ExtraTime.UnitTests.Attributes;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExtraTime.UnitTests.Functions.Orchestrators;

[TestCategory("Significant")]
public sealed class SyncFootballDataOrchestratorTests
{
    [Test]
    public async Task RunOrchestrator_NewCompetition_SetupActivitiesRunBeforeMatchSync()
    {
        var context = CreateContext(new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc));
        ConfigureCoreActivities(context, [2021, 2014], [2014]);
        ConfigureBatchActivities(
            context,
            id => new MatchSyncResult(id, false),
            id => new StandingsSyncResult(id, false));

        await SyncFootballDataOrchestrator.RunOrchestrator(context);

        var standingsIndex = context.ActivityCallNames.IndexOf(nameof(ExtraTime.Functions.Activities.SyncCompetitionStandingsActivity));
        var teamsIndex = context.ActivityCallNames.IndexOf(nameof(ExtraTime.Functions.Activities.SyncCompetitionTeamsActivity));
        var matchesIndex = context.ActivityCallNames.IndexOf(nameof(ExtraTime.Functions.Activities.SyncCompetitionMatchesActivity));

        await Assert.That(standingsIndex).IsGreaterThan(-1);
        await Assert.That(teamsIndex).IsGreaterThan(-1);
        await Assert.That(matchesIndex).IsGreaterThan(-1);
        await Assert.That(standingsIndex).IsLessThan(matchesIndex);
        await Assert.That(teamsIndex).IsLessThan(matchesIndex);
    }

    [Test]
    public async Task RunOrchestrator_At5Am_SyncsStandingsForAllExistingCompetitions()
    {
        var context = CreateContext(new DateTime(2026, 2, 1, 5, 0, 0, DateTimeKind.Utc));
        ConfigureCoreActivities(context, [2021, 2014], []);
        ConfigureBatchActivities(
            context,
            id => new MatchSyncResult(id, false),
            id => new StandingsSyncResult(id, false));

        await SyncFootballDataOrchestrator.RunOrchestrator(context);

        var standingsCalls = context.ActivityCallNames.Count(n => n == nameof(ExtraTime.Functions.Activities.SyncCompetitionStandingsActivity));
        var teamCalls = context.ActivityCallNames.Count(n => n == nameof(ExtraTime.Functions.Activities.SyncCompetitionTeamsActivity));

        await Assert.That(standingsCalls).IsEqualTo(2);
        await Assert.That(teamCalls).IsEqualTo(0);
    }

    [Test]
    public async Task RunOrchestrator_Non5AmWithoutFinishedMatches_SkipsStandingsPhase()
    {
        var context = CreateContext(new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc));
        ConfigureCoreActivities(context, [2021, 2014], []);
        ConfigureBatchActivities(
            context,
            id => new MatchSyncResult(id, false),
            id => new StandingsSyncResult(id, false));

        await SyncFootballDataOrchestrator.RunOrchestrator(context);

        var standingsCalls = context.ActivityCallNames.Count(n => n == nameof(ExtraTime.Functions.Activities.SyncCompetitionStandingsActivity));
        await Assert.That(standingsCalls).IsEqualTo(0);
    }

    private static FakeTaskOrchestrationContext CreateContext(DateTime currentUtcDateTime)
    {
        return new FakeTaskOrchestrationContext(currentUtcDateTime);
    }

    private static void ConfigureCoreActivities(
        FakeTaskOrchestrationContext context,
        List<int> competitionIds,
        List<int> setupCompetitionIds)
    {
        context.RegisterActivity<object>(
            nameof(ExtraTime.Functions.Activities.SyncCompetitionsActivity),
            _ => new object());
        context.RegisterActivity(
            nameof(ExtraTime.Functions.Activities.GetCompetitionIdsActivity),
            _ => competitionIds);
        context.RegisterActivity(
            nameof(ExtraTime.Functions.Activities.GetCompetitionsWithoutSeasonActivity),
            _ => setupCompetitionIds);
    }

    private static void ConfigureBatchActivities(
        FakeTaskOrchestrationContext context,
        Func<int, MatchSyncResult> matchResultFactory,
        Func<int, StandingsSyncResult> standingsResultFactory)
    {
        context.RegisterActivity(
            nameof(ExtraTime.Functions.Activities.SyncCompetitionMatchesActivity),
            input =>
            {
                var competitionId = (int)input!;
                return matchResultFactory(competitionId);
            });
        context.RegisterActivity(
            nameof(ExtraTime.Functions.Activities.SyncCompetitionStandingsActivity),
            input =>
            {
                var competitionId = (int)input!;
                return standingsResultFactory(competitionId);
            });
        context.RegisterActivity<object>(
            nameof(ExtraTime.Functions.Activities.SyncCompetitionTeamsActivity),
            _ => new object());
    }

    private sealed class FakeTaskOrchestrationContext(DateTime currentUtcDateTime) : TaskOrchestrationContext
    {
        private readonly Dictionary<string, Func<object?, object?>> _activityHandlers = new();

        public List<string> ActivityCallNames { get; } = [];

        public override TaskName Name => new(nameof(SyncFootballDataOrchestrator));
        public override string InstanceId => "test-instance";
        public override ParentOrchestrationInstance? Parent => null;
        public override DateTime CurrentUtcDateTime => currentUtcDateTime;
        public override bool IsReplaying => false;
        protected override ILoggerFactory LoggerFactory => NullLoggerFactory.Instance;

        public void RegisterActivity<TResult>(string activityName, Func<object?, TResult> handler)
        {
            _activityHandlers[activityName] = input => handler(input);
        }

        public void RegisterActivity<TResult>(string activityName, Func<object?, Task<TResult>> handler)
        {
            _activityHandlers[activityName] = input => handler(input).GetAwaiter().GetResult();
        }

        public override T GetInput<T>()
        {
            throw new NotSupportedException();
        }

        public override Task<TResult> CallActivityAsync<TResult>(
            TaskName name,
            object? input = null,
            TaskOptions? options = null)
        {
            ActivityCallNames.Add(name.Name);

            if (!_activityHandlers.TryGetValue(name.Name, out var handler))
            {
                throw new InvalidOperationException($"No handler configured for activity '{name.Name}'.");
            }

            var result = handler(input);
            return Task.FromResult((TResult)result!);
        }

        public override Task CreateTimer(DateTime fireAt, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task<T> WaitForExternalEvent<T>(string eventName, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public override void SendEvent(string instanceId, string eventName, object eventPayload)
        {
            throw new NotSupportedException();
        }

        public override void SetCustomStatus(object customStatus)
        {
        }

        public override Task<TResult> CallSubOrchestratorAsync<TResult>(
            TaskName orchestratorName,
            object? input = null,
            TaskOptions? options = null)
        {
            throw new NotSupportedException();
        }

        public override void ContinueAsNew(object input = null!, bool preserveUnprocessedEvents = true)
        {
            throw new NotSupportedException();
        }

        public override Guid NewGuid()
        {
            return Guid.NewGuid();
        }
    }
}
