using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExtraTime.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services.ExternalData;

public sealed class FplInjuryStatusProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<FplInjuryStatusProvider> logger) : IFplInjuryStatusProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<FplPlayerInjuryStatus>> GetCurrentStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("Fpl");
        using var response = await client.GetAsync("/api/bootstrap-static/", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("FPL bootstrap-static request failed with status {StatusCode}", response.StatusCode);
            return [];
        }

        FplBootstrapResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<FplBootstrapResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed parsing FPL bootstrap-static response.");
            return [];
        }

        return payload?.Elements?
            .Select(player => new FplPlayerInjuryStatus(
                ExternalPlayerId: player.Id,
                FplTeamId: player.Team,
                PlayerName: player.WebName ?? string.Empty,
                Status: player.Status ?? string.Empty,
                News: player.News,
                NewsUpdatedAtUtc: player.NewsAdded))
            .ToList() ?? [];
    }

    internal sealed class FplBootstrapResponse
    {
        [JsonPropertyName("elements")]
        public List<FplPlayer>? Elements { get; set; }
    }

    internal sealed class FplPlayer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("team")]
        public int Team { get; set; }

        [JsonPropertyName("web_name")]
        public string? WebName { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("news")]
        public string? News { get; set; }

        [JsonPropertyName("news_added")]
        public DateTime? NewsAdded { get; set; }
    }
}
