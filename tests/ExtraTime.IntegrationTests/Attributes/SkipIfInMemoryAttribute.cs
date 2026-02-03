using TUnit.Core;

namespace ExtraTime.IntegrationTests.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class SkipIfInMemoryAttribute() : SkipAttribute("Skipping test in InMemory mode")
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        // Check if we are NOT using SQL Server (which means we are in InMemory mode)
        return Task.FromResult(!TestConfig.UseSqlServer);
    }
}
