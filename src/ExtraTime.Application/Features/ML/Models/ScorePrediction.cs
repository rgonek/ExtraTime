namespace ExtraTime.Application.Features.ML.Models;

public sealed class ScorePrediction
{
    public float ScoreHome { get; set; }
    public float ScoreAway { get; set; }
    public float HomeConfidence { get; set; }
    public float AwayConfidence { get; set; }

    public int PredictedHomeScore => Math.Clamp((int)Math.Round(ScoreHome), 0, 5);
    public int PredictedAwayScore => Math.Clamp((int)Math.Round(ScoreAway), 0, 5);

    public string PredictedOutcome =>
        PredictedHomeScore > PredictedAwayScore ? "HomeWin" :
        PredictedHomeScore == PredictedAwayScore ? "Draw" :
        "AwayWin";

    public string PredictedScore => $"{PredictedHomeScore}-{PredictedAwayScore}";
}
