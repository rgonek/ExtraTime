namespace ExtraTime.Domain.Enums;

public enum IntegrationType
{
    FootballDataOrg = 0,
    Understat = 1,
    FootballDataUk = 2,
    ApiFootball = 3,
    ClubElo = 4,
    LineupProvider = 5,
    SuspensionProvider = 6
}

public static class IntegrationTypeExtensions
{
    public static string ToName(this IntegrationType type) => type switch
    {
        IntegrationType.FootballDataOrg => "Football-Data.org",
        IntegrationType.Understat => "Understat",
        IntegrationType.FootballDataUk => "Football-Data.co.uk",
        IntegrationType.ApiFootball => "API-Football",
        IntegrationType.ClubElo => "ClubElo.com",
        IntegrationType.LineupProvider => "Lineup Provider",
        IntegrationType.SuspensionProvider => "Suspension Provider",
        _ => type.ToString()
    };

    public static TimeSpan GetStaleThreshold(this IntegrationType type) => type switch
    {
        IntegrationType.FootballDataOrg => TimeSpan.FromHours(6),
        IntegrationType.Understat => TimeSpan.FromHours(48),
        IntegrationType.FootballDataUk => TimeSpan.FromDays(7),
        IntegrationType.ApiFootball => TimeSpan.FromHours(48),
        IntegrationType.ClubElo => TimeSpan.FromHours(48),
        IntegrationType.LineupProvider => TimeSpan.FromHours(24),
        IntegrationType.SuspensionProvider => TimeSpan.FromHours(24),
        _ => TimeSpan.FromHours(24)
    };
}
