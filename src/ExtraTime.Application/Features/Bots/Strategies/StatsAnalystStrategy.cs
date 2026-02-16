using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Application.Features.Bots.Strategies;

public sealed class StatsAnalystStrategy : IBotBettingStrategy
{
    private readonly ITeamFormCalculator _formCalculator;
    private readonly IIntegrationHealthService? _integrationHealthService;
    private readonly IUnderstatService? _understatService;
    private readonly IOddsDataService? _oddsService;
    private readonly IInjuryService? _injuryService;
    private readonly IEloRatingService? _eloService;
    private readonly ILogger<StatsAnalystStrategy>? _logger;
    private readonly FallbackStrategy _fallbackStrategy;
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.StatsAnalyst;

    public StatsAnalystStrategy(
        ITeamFormCalculator formCalculator,
        IApplicationDbContext? context = null,
        IIntegrationHealthService? integrationHealthService = null,
        IUnderstatService? understatService = null,
        IOddsDataService? oddsService = null,
        IInjuryService? injuryService = null,
        IEloRatingService? eloService = null,
        ILogger<StatsAnalystStrategy>? logger = null,
        FallbackStrategy? fallbackStrategy = null)
    {
        _formCalculator = formCalculator;
        _integrationHealthService = integrationHealthService;
        _understatService = understatService;
        _oddsService = oddsService;
        _injuryService = injuryService;
        _eloService = eloService;
        _logger = logger;
        _fallbackStrategy = fallbackStrategy ?? new FallbackStrategy();
    }

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        return _fallbackStrategy.GenerateBasicPrediction();
    }

    public async Task<(int HomeScore, int AwayScore)> GeneratePredictionAsync(
        Match match,
        string? configuration,
        CancellationToken cancellationToken = default)
    {
        var config = StatsAnalystConfig.FromJson(configuration);
        var dataAvailability = await GetDataAvailabilityAsync(cancellationToken);
        var predictionContext = await BuildPredictionContextAsync(
            match,
            config,
            dataAvailability,
            cancellationToken);

        var warning = predictionContext.GetDegradationWarning();
        if (warning is not null)
        {
            _logger?.LogWarning("StatsAnalyst degraded for match {MatchId}: {Warning}", match.Id, warning);
        }

        if (!predictionContext.CanMakePrediction())
        {
            _logger?.LogWarning("StatsAnalyst fallback prediction used for match {MatchId}", match.Id);
            return _fallbackStrategy.GenerateBasicPrediction();
        }

        var effectiveWeights = predictionContext.CalculateEffectiveWeights();
        var (expectedHome, expectedAway) = CalculateExpectedGoals(
            match,
            predictionContext,
            effectiveWeights,
            config);

        return ConvertToScoreline(expectedHome, expectedAway, config);
    }

    private async Task<PredictionContext> BuildPredictionContextAsync(
        Match match,
        StatsAnalystConfig config,
        DataAvailability dataAvailability,
        CancellationToken cancellationToken)
    {
        var homeForm = await _formCalculator.CalculateFormAsync(
            match.HomeTeamId,
            match.CompetitionId,
            config.MatchesAnalyzed,
            cancellationToken);
        var awayForm = await _formCalculator.CalculateFormAsync(
            match.AwayTeamId,
            match.CompetitionId,
            config.MatchesAnalyzed,
            cancellationToken);

        var context = new PredictionContext
        {
            Match = match,
            Config = config,
            DataAvailability = dataAvailability,
            HomeForm = homeForm,
            AwayForm = awayForm
        };

        if (config.UseXgData && dataAvailability.XgDataAvailable && _understatService is not null)
        {
            var season = ResolveSeason(match.MatchDateUtc);
            context.HomeXg = await _understatService.GetTeamXgAsync(
                match.HomeTeamId,
                match.CompetitionId,
                season,
                cancellationToken);
            context.AwayXg = await _understatService.GetTeamXgAsync(
                match.AwayTeamId,
                match.CompetitionId,
                season,
                cancellationToken);
        }

        if (config.UseOddsData && dataAvailability.OddsDataAvailable && _oddsService is not null)
        {
            context.Odds = await _oddsService.GetOddsForMatchAsync(match.Id, cancellationToken);
        }

        if (config.UseInjuryData && dataAvailability.InjuryDataAvailable && _injuryService is not null)
        {
            context.HomeInjuries = await _injuryService.GetTeamInjuriesAsync(match.HomeTeamId, cancellationToken);
            context.AwayInjuries = await _injuryService.GetTeamInjuriesAsync(match.AwayTeamId, cancellationToken);
        }

        if (config.UseEloData && dataAvailability.EloDataAvailable && _eloService is not null)
        {
            context.HomeElo = await _eloService.GetTeamEloAsync(match.HomeTeamId, cancellationToken);
            context.AwayElo = await _eloService.GetTeamEloAsync(match.AwayTeamId, cancellationToken);
        }

        return context;
    }

    private async Task<DataAvailability> GetDataAvailabilityAsync(CancellationToken cancellationToken)
    {
        if (_integrationHealthService is not null)
        {
            return await _integrationHealthService.GetDataAvailabilityAsync(cancellationToken);
        }

        return new DataAvailability
        {
            FormDataAvailable = true,
            XgDataAvailable = _understatService is not null,
            OddsDataAvailable = _oddsService is not null,
            InjuryDataAvailable = _injuryService is not null,
            LineupDataAvailable = false,
            EloDataAvailable = _eloService is not null,
            StandingsDataAvailable = true
        };
    }

    private (double HomeExpectedGoals, double AwayExpectedGoals) CalculateExpectedGoals(
        Match match,
        PredictionContext context,
        EffectiveWeights effectiveWeights,
        StatsAnalystConfig config)
    {
        var homeExpected = 1.5;
        var awayExpected = 1.2;

        if (context.CanUseForm)
        {
            var homeFormModifier = context.HomeForm!.GetFormScore() / 50.0;
            var awayFormModifier = context.AwayForm!.GetFormScore() / 50.0;
            homeExpected *= 1 + ((homeFormModifier - 1) * effectiveWeights.FormWeight);
            awayExpected *= 1 + ((awayFormModifier - 1) * effectiveWeights.FormWeight);

            if (config.GoalTrendWeight > 0)
            {
                homeExpected = homeExpected * (1 - config.GoalTrendWeight) + context.HomeForm.GoalsPerMatch * config.GoalTrendWeight;
                awayExpected = awayExpected * (1 - config.GoalTrendWeight) + context.AwayForm.GoalsPerMatch * config.GoalTrendWeight;
            }

            if (config.StreakWeight > 0)
            {
                homeExpected *= 1 + (context.HomeForm.CurrentStreak * 0.02 * config.StreakWeight);
                awayExpected *= 1 + (context.AwayForm.CurrentStreak * 0.02 * config.StreakWeight);
            }
        }

        if (effectiveWeights.HomeAdvantageWeight > 0)
        {
            homeExpected *= 1 + (0.15 * effectiveWeights.HomeAdvantageWeight);
            awayExpected *= 1 - (0.10 * effectiveWeights.HomeAdvantageWeight);
        }

        if (context.CanUseXg)
        {
            homeExpected = homeExpected * (1 - effectiveWeights.XgWeight) + context.HomeXg!.XgPerMatch * effectiveWeights.XgWeight;
            awayExpected = awayExpected * (1 - effectiveWeights.XgWeight) + context.AwayXg!.XgPerMatch * effectiveWeights.XgWeight;

            if (effectiveWeights.XgDefensiveWeight > 0)
            {
                homeExpected *= 1 + ((context.AwayXg.XgAgainstPerMatch - 1.3) * effectiveWeights.XgDefensiveWeight);
                awayExpected *= 1 + ((context.HomeXg.XgAgainstPerMatch - 1.3) * effectiveWeights.XgDefensiveWeight);
            }
        }

        if (context.CanUseOdds)
        {
            if (context.Odds!.MarketFavorite == MatchOutcome.HomeWin)
            {
                homeExpected *= 1 + ((context.Odds.FavoriteConfidence - 0.4) * effectiveWeights.OddsWeight);
            }
            else if (context.Odds.MarketFavorite == MatchOutcome.AwayWin)
            {
                awayExpected *= 1 + ((context.Odds.FavoriteConfidence - 0.3) * effectiveWeights.OddsWeight);
            }
        }

        if (context.CanUseInjuries)
        {
            if (context.HomeInjuries is not null)
            {
                homeExpected *= 1 - ((context.HomeInjuries.InjuryImpactScore / 100.0) * effectiveWeights.InjuryWeight);
            }

            if (context.AwayInjuries is not null)
            {
                awayExpected *= 1 - ((context.AwayInjuries.InjuryImpactScore / 100.0) * effectiveWeights.InjuryWeight);
            }
        }

        if (context.CanUseElo)
        {
            var eloDiff = context.HomeElo!.EloRating - context.AwayElo!.EloRating;
            var normalizedEloDiff = eloDiff / 400.0;
            homeExpected *= 1 + (normalizedEloDiff * effectiveWeights.EloWeight);
            awayExpected *= 1 - (normalizedEloDiff * effectiveWeights.EloWeight);
        }

        var isLateSeasonMatch = match.Matchday >= config.LateSeasonMatchday;
        if (isLateSeasonMatch && config.HighStakesBoost)
        {
            var average = (homeExpected + awayExpected) / 2;
            homeExpected = homeExpected * 0.85 + average * 0.15;
            awayExpected = awayExpected * 0.85 + average * 0.15;
        }

        homeExpected = Math.Max(0.05, homeExpected);
        awayExpected = Math.Max(0.05, awayExpected);
        return (homeExpected, awayExpected);
    }

    private (int HomeScore, int AwayScore) ConvertToScoreline(
        double homeExpectedGoals,
        double awayExpectedGoals,
        StatsAnalystConfig config)
    {
        if (config.RandomVariance > 0)
        {
            homeExpectedGoals += (_random.NextDouble() - 0.5) * 2 * config.RandomVariance * homeExpectedGoals;
            awayExpectedGoals += (_random.NextDouble() - 0.5) * 2 * config.RandomVariance * awayExpectedGoals;
        }

        var homeScore = config.Style switch
        {
            PredictionStyle.Conservative => (int)Math.Floor(homeExpectedGoals),
            PredictionStyle.Bold => (int)Math.Ceiling(homeExpectedGoals),
            _ => (int)Math.Round(homeExpectedGoals)
        };

        var awayScore = config.Style switch
        {
            PredictionStyle.Conservative => (int)Math.Floor(awayExpectedGoals),
            PredictionStyle.Bold => (int)Math.Ceiling(awayExpectedGoals),
            _ => (int)Math.Round(awayExpectedGoals)
        };

        homeScore = Math.Clamp(homeScore, config.MinGoals, config.MaxGoals);
        awayScore = Math.Clamp(awayScore, config.MinGoals, config.MaxGoals);
        return (homeScore, awayScore);
    }

    private static string ResolveSeason(DateTime matchDateUtc)
    {
        return matchDateUtc.Month < 8
            ? (matchDateUtc.Year - 1).ToString()
            : matchDateUtc.Year.ToString();
    }
}
