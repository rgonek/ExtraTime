namespace ExtraTime.Infrastructure.Configuration;

public sealed class LineupQualityGatePolicy
{
    public bool EnforceBeforeInjurySync { get; set; }
    public double MinLineupCoverageUpcoming24hPercent { get; set; } = 75;
    public double MinLineupCoveragePlayedMatchesPercent { get; set; } = 90;
    public double MaxLineupParseFailureRatePercent { get; set; } = 5;
}
