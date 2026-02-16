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
    private readonly ISuspensionService? _suspensionService;
    private readonly IWeatherContextService? _weatherContextService;
    private readonly IRefereeProfileService? _refereeProfileService;
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
        ISuspensionService? suspensionService = null,
        IWeatherContextService? weatherContextService = null,
        IRefereeProfileService? refereeProfileService = null,
        IEloRatingService? eloService = null,
        ILogger<StatsAnalystStrategy>? logger = null,
        FallbackStrategy? fallbackStrategy = null)
    {
        _formCalculator = formCalculator;
        _integrationHealthService = integrationHealthService;
        _understatService = understatService;
        _oddsService = oddsService;
        _injuryService = injuryService;
        _suspensionService = suspensionService;
        _weatherContextService = weatherContextService;
        _refereeProfileService = refereeProfileService;
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
        LogSourceQuality(match, config, predictionContext);

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
            var asOfUtc = match.MatchDateUtc;
            context.HomeXg = await _understatService.GetTeamXgAsOfAsync(
                match.HomeTeamId,
                match.CompetitionId,
                asOfUtc,
                cancellationToken);
            context.AwayXg = await _understatService.GetTeamXgAsOfAsync(
                match.AwayTeamId,
                match.CompetitionId,
                asOfUtc,
                cancellationToken);
        }

        if (config.UseOddsData && dataAvailability.OddsDataAvailable && _oddsService is not null)
        {
            context.Odds = await _oddsService.GetOddsForMatchAsOfAsync(match.Id, match.MatchDateUtc, cancellationToken);
        }

        if (config.UseInjuryData && dataAvailability.InjuryDataAvailable && _injuryService is not null)
        {
            context.HomeInjuries = await _injuryService.GetTeamInjuriesAsOfAsync(
                match.HomeTeamId,
                match.MatchDateUtc,
                cancellationToken);
            context.AwayInjuries = await _injuryService.GetTeamInjuriesAsOfAsync(
                match.AwayTeamId,
                match.MatchDateUtc,
                cancellationToken);
        }

        if (dataAvailability.SuspensionDataAvailable && _suspensionService is not null)
        {
            context.HomeSuspensions = await _suspensionService.GetTeamSuspensionsAsOfAsync(
                match.HomeTeamId,
                match.MatchDateUtc,
                cancellationToken);
            context.AwaySuspensions = await _suspensionService.GetTeamSuspensionsAsOfAsync(
                match.AwayTeamId,
                match.MatchDateUtc,
                cancellationToken);
        }

        if (config.UseEloData && dataAvailability.EloDataAvailable && _eloService is not null)
        {
            context.HomeElo = await _eloService.GetTeamEloAtDateAsync(
                match.HomeTeamId,
                match.MatchDateUtc,
                cancellationToken);
            context.AwayElo = await _eloService.GetTeamEloAtDateAsync(
                match.AwayTeamId,
                match.MatchDateUtc,
                cancellationToken);
        }

        if (dataAvailability.WeatherDataAvailable && _weatherContextService is not null)
        {
            context.WeatherContext = await _weatherContextService.GetWeatherContextAsync(
                match.Id,
                match.MatchDateUtc,
                cancellationToken);
        }

        if (dataAvailability.RefereeDataAvailable && _refereeProfileService is not null)
        {
            context.RefereeProfile = await _refereeProfileService.GetRefereeProfileAsync(
                match.Id,
                match.MatchDateUtc,
                cancellationToken);
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
            SuspensionDataAvailable = _suspensionService is not null,
            LineupDataAvailable = false,
            EloDataAvailable = _eloService is not null,
            WeatherDataAvailable = _weatherContextService is not null,
            RefereeDataAvailable = _refereeProfileService is not null,
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

    private void LogSourceQuality(
        Match match,
        StatsAnalystConfig config,
        PredictionContext predictionContext)
    {
        var trackedSources = new List<(string Name, bool Enabled, bool Available)>
        {
            ("xg", config.UseXgData, predictionContext.HomeXg is not null && predictionContext.AwayXg is not null),
            ("odds", config.UseOddsData, predictionContext.Odds is not null),
            ("injuries", config.UseInjuryData, predictionContext.HomeInjuries is not null || predictionContext.AwayInjuries is not null),
            ("suspensions", predictionContext.DataAvailability.SuspensionDataAvailable, predictionContext.HasSuspensionContext),
            ("elo", config.UseEloData, predictionContext.HomeElo is not null && predictionContext.AwayElo is not null),
            ("weather", predictionContext.DataAvailability.WeatherDataAvailable, predictionContext.HasWeatherContext),
            ("referee", predictionContext.DataAvailability.RefereeDataAvailable, predictionContext.HasRefereeContext)
        };

        var enabledSources = trackedSources.Where(x => x.Enabled).ToList();
        if (enabledSources.Count == 0)
        {
            return;
        }

        var missingSources = enabledSources.Where(x => !x.Available).Select(x => x.Name).ToArray();
        var missingRate = (double)missingSources.Length / enabledSources.Count * 100;

        _logger?.LogInformation(
            "StatsAnalyst source quality for match {MatchId}: missing rate {MissingRate:F1}% ({Missing}/{Enabled}) [{MissingSources}]",
            match.Id,
            missingRate,
            missingSources.Length,
            enabledSources.Count,
            missingSources.Length > 0 ? string.Join(", ", missingSources) : "none");
    }
}
