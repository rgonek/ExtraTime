using TUnit.Core;

namespace ExtraTime.API.Tests.Attributes;

public sealed class SkipOnGitHubActionsAttribute() : SkipAttribute("Skipping test on GitHub Actions")
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        var isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
        return Task.FromResult(isGitHubActions);
    }
}
