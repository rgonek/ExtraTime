using ExtraTime.Domain.Common;

namespace ExtraTime.API.Features.Health;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => Results.Ok(new HealthResponse(
            Status: "Healthy",
            Timestamp: Clock.UtcNow,
            Version: "1.0.0"
        )))
        .WithName("GetHealth")
        .WithTags("Health");
    }
}

public sealed record HealthResponse(string Status, DateTime Timestamp, string Version);
