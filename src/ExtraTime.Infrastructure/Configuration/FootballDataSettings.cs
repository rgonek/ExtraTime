namespace ExtraTime.Infrastructure.Configuration;

public sealed class FootballDataSettings
{
    public const string SectionName = "FootballData";

    public required string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.football-data.org/v4";
    public int RequestsPerMinute { get; set; } = 10;
    public int[] SupportedCompetitionIds { get; set; } = [];
}
