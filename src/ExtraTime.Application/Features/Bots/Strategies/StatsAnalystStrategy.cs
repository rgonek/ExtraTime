using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.ValueObjects;

namespace ExtraTime.Application.Features.Bots.Strategies;

public sealed class StatsAnalystStrategy : IBotBettingStrategy
{
    private readonly ITeamFormCalculator _formCalculator;
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.StatsAnalyst;

    public StatsAnalystStrategy(ITeamFormCalculator formCalculator)
    {
        _formCalculator = formCalculator;
    }

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        var config = StatsAnalystConfig.FromJson(configuration);

        var homeForm = GetFormSync(match.HomeTeamId, match.CompetitionId, config.MatchesAnalyzed);
        var awayForm = GetFormSync(match.AwayTeamId, match.CompetitionId, config.MatchesAnalyzed);

        var analysis = AnalyzeMatch(match, homeForm, awayForm, config);

        return (analysis.PredictedHomeScore, analysis.PredictedAwayScore);
    }

    public async Task<(int HomeScore, int AwayScore)> GeneratePredictionAsync(
        Match match,
        string? configuration,
        CancellationToken cancellationToken = default)
    {
        var config = StatsAnalystConfig.FromJson(configuration);

        var homeForm = await _formCalculator.CalculateFormAsync(
            match.HomeTeamId, match.CompetitionId, config.MatchesAnalyzed, cancellationToken);
        var awayForm = await _formCalculator.CalculateFormAsync(
            match.AwayTeamId, match.CompetitionId, config.MatchesAnalyzed, cancellationToken);

        var analysis = AnalyzeMatch(match, homeForm, awayForm, config);

        return (analysis.PredictedHomeScore, analysis.PredictedAwayScore);
    }

    private MatchAnalysis AnalyzeMatch(
        Match match,
        TeamFormCache homeForm,
        TeamFormCache awayForm,
        StatsAnalystConfig config)
    {
        double homeFormScore = homeForm.GetFormScore() * config.FormWeight;
        double awayFormScore = awayForm.GetFormScore() * config.FormWeight;

        double homeAdvantage = homeForm.GetHomeStrength() * config.HomeAdvantageWeight * 100;
        double awayPenalty = (1 - awayForm.GetAwayStrength()) * config.HomeAdvantageWeight * 50;

        double expectedHomeGoals = (homeForm.GetAttackStrength() + (1 / Math.Max(0.5, awayForm.GetDefenseStrength()))) / 2;
        double expectedAwayGoals = (awayForm.GetAttackStrength() + (1 / Math.Max(0.5, homeForm.GetDefenseStrength()))) / 2;

        expectedHomeGoals *= (1 + config.GoalTrendWeight);
        expectedAwayGoals *= (1 + config.GoalTrendWeight * 0.8);

        if (config.StreakWeight > 0)
        {
            double homeStreakBonus = homeForm.CurrentStreak * 0.1 * config.StreakWeight;
            double awayStreakBonus = awayForm.CurrentStreak * 0.1 * config.StreakWeight;
            expectedHomeGoals += homeStreakBonus;
            expectedAwayGoals += awayStreakBonus;
        }

        bool isLateSeasonMatch = match.Matchday >= config.LateSeasonMatchday;
        bool isHighStakes = isLateSeasonMatch;

        if (isHighStakes && config.HighStakesBoost)
        {
            double avgGoals = (expectedHomeGoals + expectedAwayGoals) / 2;
            expectedHomeGoals = expectedHomeGoals * 0.85 + avgGoals * 0.15;
            expectedAwayGoals = expectedAwayGoals * 0.85 + avgGoals * 0.15;
        }

        double homeFinalScore = homeFormScore + homeAdvantage - awayPenalty;
        double awayFinalScore = awayFormScore - homeAdvantage * 0.5;

        int predictedHomeGoals = ConvertToGoals(expectedHomeGoals, homeFinalScore, config);
        int predictedAwayGoals = ConvertToGoals(expectedAwayGoals, awayFinalScore, config);

        if (config.RandomVariance > 0)
        {
            predictedHomeGoals = ApplyVariance(predictedHomeGoals, config);
            predictedAwayGoals = ApplyVariance(predictedAwayGoals, config);
        }

        predictedHomeGoals = Math.Clamp(predictedHomeGoals, config.MinGoals, config.MaxGoals);
        predictedAwayGoals = Math.Clamp(predictedAwayGoals, config.MinGoals, config.MaxGoals);

        return new MatchAnalysis
        {
            Match = match,
            HomeTeamForm = homeForm,
            AwayTeamForm = awayForm,
            HomeFormScore = homeFormScore,
            AwayFormScore = awayFormScore,
            HomeAdvantage = homeAdvantage,
            ExpectedHomeGoals = expectedHomeGoals,
            ExpectedAwayGoals = expectedAwayGoals,
            IsHighStakes = isHighStakes,
            IsLateSeasonMatch = isLateSeasonMatch,
            PredictedHomeScore = predictedHomeGoals,
            PredictedAwayScore = predictedAwayGoals
        };
    }

    private int ConvertToGoals(double expectedGoals, double formScore, StatsAnalystConfig config)
    {
        double adjustedGoals = expectedGoals;

        if (formScore > 50)
        {
            adjustedGoals *= 1 + ((formScore - 50) / 200);
        }
        else
        {
            adjustedGoals *= 1 - ((50 - formScore) / 200);
        }

        return config.Style switch
        {
            PredictionStyle.Conservative => (int)Math.Floor(adjustedGoals),
            PredictionStyle.Bold => (int)Math.Ceiling(adjustedGoals),
            _ => (int)Math.Round(adjustedGoals)
        };
    }

    private int ApplyVariance(int goals, StatsAnalystConfig config)
    {
        double variance = (_random.NextDouble() - 0.5) * 2 * config.RandomVariance;
        int adjustment = (int)Math.Round(variance * 2);
        return goals + adjustment;
    }

    private TeamFormCache GetFormSync(Guid teamId, Guid competitionId, int matchesAnalyzed)
    {
        return new TeamFormCache
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            CompetitionId = competitionId,
            MatchesAnalyzed = matchesAnalyzed,
            PointsPerMatch = 1.5,
            GoalsPerMatch = 1.5,
            GoalsConcededPerMatch = 1.2,
            HomeWinRate = 0.45,
            AwayWinRate = 0.30,
            RecentForm = "",
            CalculatedAt = DateTime.UtcNow
        };
    }
}
