namespace ExtraTime.MLTrainer.Models;

public sealed record TrainingResult(
    string Version,
    int TrainingSamples,
    ModelMetricsResult HomeModelMetrics,
    ModelMetricsResult AwayModelMetrics);

public sealed record ModelMetricsResult(
    double RSquared,
    double MeanAbsoluteError,
    double RootMeanSquaredError);
