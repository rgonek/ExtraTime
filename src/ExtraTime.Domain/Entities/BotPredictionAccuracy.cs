using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Entities;

public sealed class BotPredictionAccuracy : BaseEntity
{
    public required Guid BotId { get; set; }
    public Bot Bot { get; set; } = null!;
    public BotStrategy Strategy { get; set; }
    public string? ModelVersion { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodType { get; set; } = "weekly";
    public int TotalPredictions { get; set; }
    public int ExactScores { get; set; }
    public int CorrectResults { get; set; }
    public int GoalsOffBy1 { get; set; }
    public int GoalsOffBy2 { get; set; }
    public int GoalsOffBy3Plus { get; set; }
    public double ExactScoreAccuracy { get; set; }
    public double CorrectResultAccuracy { get; set; }
    public double Within1GoalAccuracy { get; set; }
    public double MeanAbsoluteError { get; set; }
    public double RootMeanSquaredError { get; set; }
    public double HomeScoreMAE { get; set; }
    public double AwayScoreMAE { get; set; }
    public double TotalPointsEarned { get; set; }
    public double AvgPointsPerBet { get; set; }
    public int BetsWon { get; set; }
    public int BetsLost { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public string? CalculationNotes { get; set; }
}
