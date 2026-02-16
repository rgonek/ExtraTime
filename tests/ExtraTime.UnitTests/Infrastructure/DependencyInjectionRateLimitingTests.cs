using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ExtraTime.Infrastructure;
using ExtraTime.UnitTests.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExtraTime.UnitTests.Infrastructure;

[TestCategory("Significant")]
public sealed class DependencyInjectionRateLimitingTests
{
    [Test]
    public async Task AddInfrastructureServices_RateLimitingEnabled_UsesPerUserTokenBuckets()
    {
        // Arrange
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["RateLimiting:Enabled"] = "true",
            ["RateLimiting:TokenLimit"] = "1",
            ["RateLimiting:TokensPerPeriod"] = "1",
            ["RateLimiting:ReplenishPeriodSeconds"] = "60",
            ["RateLimiting:QueueLimit"] = "0",
            ["RateLimiting:AutoReplenishment"] = "false"
        });
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        var firstUserContext = CreateUserContext(Guid.NewGuid());
        var secondUserContext = CreateUserContext(Guid.NewGuid());

        // Act
        using var firstUserFirstRequest = await options.GlobalLimiter!.AcquireAsync(firstUserContext, 1);
        using var firstUserSecondRequest = await options.GlobalLimiter.AcquireAsync(firstUserContext, 1);
        using var secondUserFirstRequest = await options.GlobalLimiter.AcquireAsync(secondUserContext, 1);

        // Assert
        await Assert.That(options.RejectionStatusCode).IsEqualTo(StatusCodes.Status429TooManyRequests);
        await Assert.That(firstUserFirstRequest.IsAcquired).IsTrue();
        await Assert.That(firstUserSecondRequest.IsAcquired).IsFalse();
        await Assert.That(secondUserFirstRequest.IsAcquired).IsTrue();
    }

    [Test]
    public async Task AddInfrastructureServices_HealthPath_BypassesRateLimiting()
    {
        // Arrange
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["RateLimiting:Enabled"] = "true",
            ["RateLimiting:TokenLimit"] = "1",
            ["RateLimiting:TokensPerPeriod"] = "1",
            ["RateLimiting:ReplenishPeriodSeconds"] = "60",
            ["RateLimiting:QueueLimit"] = "0",
            ["RateLimiting:AutoReplenishment"] = "false"
        });
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";

        // Act
        using var firstRequest = await options.GlobalLimiter!.AcquireAsync(context, 1);
        using var secondRequest = await options.GlobalLimiter.AcquireAsync(context, 1);

        // Assert
        await Assert.That(firstRequest.IsAcquired).IsTrue();
        await Assert.That(secondRequest.IsAcquired).IsTrue();
    }

    [Test]
    public async Task AddInfrastructureServices_RateLimitingDisabled_LeavesGlobalLimiterUnset()
    {
        // Arrange
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["RateLimiting:Enabled"] = "false"
        });

        // Act
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        // Assert
        await Assert.That(options.GlobalLimiter).IsNull();
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> overrides)
    {
        var baseSettings = new Dictionary<string, string?>
        {
            ["UseInMemoryDatabase"] = "true"
        };

        foreach (var item in overrides)
        {
            baseSettings[item.Key] = item.Value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(baseSettings)
            .Build();
        var services = new ServiceCollection();
        services.AddInfrastructureServices(configuration);

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateUserContext(Guid userId)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/auth/me";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
        ], "TestAuth"));

        return context;
    }
}
