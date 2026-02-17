using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class MlModelVersion : BaseEntity
{
    public required string ModelType { get; set; }
    public required string Version { get; set; }
    public string? BlobPath { get; set; }
    public DateTime TrainedAt { get; set; }
    public int TrainingSamples { get; set; }
    public string? TrainingDataRange { get; set; }
    public double Rsquared { get; set; }
    public double MeanAbsoluteError { get; set; }
    public double RootMeanSquaredError { get; set; }
    public double MeanAbsolutePercentageError { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? ActivationNotes { get; set; }
    public string? FeatureImportanceJson { get; set; }
    public string? AlgorithmUsed { get; set; }
    public string? HyperparametersJson { get; set; }
    public double CrossValidationMAE { get; set; }
    public double CrossValidationStdDev { get; set; }
}
