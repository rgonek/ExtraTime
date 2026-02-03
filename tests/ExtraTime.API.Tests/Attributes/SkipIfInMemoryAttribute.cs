using TUnit.Core;
using ExtraTime.API.Tests.Fixtures;

namespace ExtraTime.API.Tests.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class SkipIfInMemoryAttribute() : SkipAttribute("Skipping test in InMemory mode")
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        return Task.FromResult(CustomWebApplicationFactory.UseInMemory);
    }
}
