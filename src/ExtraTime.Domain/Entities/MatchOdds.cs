using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Entities;

/// <summary>
/// Historical betting odds for a match.
/// Data sourced from Football-Data.co.uk CSV files.
/// </summary>
public sealed class MatchOdds : BaseEntity
{
    public required Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public double HomeWinOdds { get; set; }
    public double DrawOdds { get; set; }
    public double AwayWinOdds { get; set; }

    public double HomeWinProbability { get; set; }
    public double DrawProbability { get; set; }
    public double AwayWinProbability { get; set; }

    public double? Over25Odds { get; set; }
    public double? Under25Odds { get; set; }

    public double? BttsYesOdds { get; set; }
    public double? BttsNoOdds { get; set; }

    public MatchOutcome MarketFavorite { get; set; }
    public double FavoriteConfidence { get; set; }

    public string DataSource { get; set; } = "football-data.co.uk";
    public DateTime ImportedAt { get; set; }

    public static double OddsToProbability(double odds) => odds > 0 ? 1d / odds : 0d;

    public void CalculateProbabilities()
    {
        var home = OddsToProbability(HomeWinOdds);
        var draw = OddsToProbability(DrawOdds);
        var away = OddsToProbability(AwayWinOdds);
        var total = home + draw + away;

        if (total <= 0)
        {
            HomeWinProbability = 0;
            DrawProbability = 0;
            AwayWinProbability = 0;
            MarketFavorite = MatchOutcome.Draw;
            FavoriteConfidence = 0;
            return;
        }

        HomeWinProbability = home / total;
        DrawProbability = draw / total;
        AwayWinProbability = away / total;

        if (HomeWinProbability >= DrawProbability && HomeWinProbability >= AwayWinProbability)
        {
            MarketFavorite = MatchOutcome.HomeWin;
            FavoriteConfidence = HomeWinProbability;
            return;
        }

        if (AwayWinProbability >= DrawProbability)
        {
            MarketFavorite = MatchOutcome.AwayWin;
            FavoriteConfidence = AwayWinProbability;
            return;
        }

        MarketFavorite = MatchOutcome.Draw;
        FavoriteConfidence = DrawProbability;
    }
}
