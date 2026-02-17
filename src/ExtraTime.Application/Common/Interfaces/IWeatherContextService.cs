namespace ExtraTime.Application.Common.Interfaces;

public interface IWeatherContextService
{
    Task<WeatherContextData?> GetWeatherContextAsync(
        Guid matchId,
        DateTime kickoffUtc,
        CancellationToken cancellationToken = default);
}

public sealed record WeatherContextData(
    double? TemperatureCelsius,
    double? WindSpeedKph,
    double? HumidityPercent,
    string? Conditions,
    bool IsExtremeWeather);
