namespace ExtraTime.API.Features.Health;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => Results.Ok(new HealthResponse(
            Status: "Healthy",
            Timestamp: DateTime.UtcNow,
            Version: "1.0.0"
        )))
        .WithName("GetHealth")
        .WithTags("Health")
        .WithOpenApi();
    }
}

public sealed record HealthResponse(string Status, DateTime Timestamp, string Version);
