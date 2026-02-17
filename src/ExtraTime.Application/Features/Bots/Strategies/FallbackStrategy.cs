namespace ExtraTime.Application.Features.Bots.Strategies;

public sealed class FallbackStrategy(Random? random = null)
{
    private readonly Random _random = random ?? Random.Shared;

    /// <summary>
    /// Simple prediction based mostly on home advantage.
    /// Used when data quality is too low for advanced analysis.
    /// </summary>
    public (int HomeScore, int AwayScore) GenerateBasicPrediction()
    {
        var homeScore = _random.NextDouble() < 0.55 ? 2 : 1;
        var awayScore = _random.NextDouble() < 0.40 ? 1 : 0;

        if (_random.NextDouble() < 0.20)
        {
            awayScore = homeScore;
        }

        return (homeScore, awayScore);
    }
}
