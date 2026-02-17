namespace ExtraTime.Application.Features.ML.Models;

public sealed class MatchFeatures
{
    public required string MatchId { get; set; }
    public required string HomeTeamId { get; set; }
    public required string AwayTeamId { get; set; }

    public float HomeFormPointsLast5 { get; set; }
    public float HomeGoalsScoredLast5 { get; set; }
    public float HomeGoalsConcededLast5 { get; set; }
    public float HomeCleanSheetsLast5 { get; set; }
    public float HomeWinsLast5 { get; set; }
    public float HomeDrawsLast5 { get; set; }
    public float HomeLossesLast5 { get; set; }
    public float HomeGoalsScoredAvg { get; set; }
    public float HomeGoalsConcededAvg { get; set; }
    public float HomePointsPerGame { get; set; }
    public float HomeGoalsScoredAtHomeAvg { get; set; }
    public float HomeGoalsConcededAtHomeAvg { get; set; }
    public float HomeWinRateAtHome { get; set; }

    public float AwayFormPointsLast5 { get; set; }
    public float AwayGoalsScoredLast5 { get; set; }
    public float AwayGoalsConcededLast5 { get; set; }
    public float AwayCleanSheetsLast5 { get; set; }
    public float AwayWinsLast5 { get; set; }
    public float AwayDrawsLast5 { get; set; }
    public float AwayLossesLast5 { get; set; }
    public float AwayGoalsScoredAvg { get; set; }
    public float AwayGoalsConcededAvg { get; set; }
    public float AwayPointsPerGame { get; set; }
    public float AwayGoalsScoredAwayAvg { get; set; }
    public float AwayGoalsConcededAwayAvg { get; set; }
    public float AwayWinRateAway { get; set; }

    public float H2HMatchesPlayed { get; set; }
    public float H2HHomeWins { get; set; }
    public float H2HAwayWins { get; set; }
    public float H2HDraws { get; set; }
    public float H2HHomeGoalsAvg { get; set; }
    public float H2HAwayGoalsAvg { get; set; }
    public float H2HBttsRate { get; set; }
    public float H2HOver2_5Rate { get; set; }
    public float H2HRecentHomeWins { get; set; }
    public float H2HRecentAwayWins { get; set; }

    public float LeagueAvgHomeGoals { get; set; }
    public float LeagueAvgAwayGoals { get; set; }
    public float LeagueHomeAdvantage { get; set; }
    public float SeasonProgress { get; set; }
    public float HomeLeaguePosition { get; set; }
    public float AwayLeaguePosition { get; set; }
    public float PositionDifference { get; set; }
    public float HomeIsTopHalf { get; set; }
    public float AwayIsTopHalf { get; set; }

    public float HomeOdds { get; set; }
    public float DrawOdds { get; set; }
    public float AwayOdds { get; set; }
    public float ImpliedHomeProbability { get; set; }
    public float ImpliedAwayProbability { get; set; }

    public float DayOfWeek { get; set; }
    public float IsWeekend { get; set; }
    public float Month { get; set; }
    public float DaysSinceLastMatchHome { get; set; }
    public float DaysSinceLastMatchAway { get; set; }

    public float HomeXgPerMatch { get; set; }
    public float HomeXgAgainstPerMatch { get; set; }
    public float HomeXgOverperformance { get; set; }
    public float HomeRecentXgPerMatch { get; set; }
    public float AwayXgPerMatch { get; set; }
    public float AwayXgAgainstPerMatch { get; set; }
    public float AwayXgOverperformance { get; set; }
    public float AwayRecentXgPerMatch { get; set; }

    public float HomeEloRating { get; set; }
    public float AwayEloRating { get; set; }
    public float EloDifference { get; set; }

    public float HomeShotsPerMatch { get; set; }
    public float HomeShotsOnTargetPerMatch { get; set; }
    public float HomeSOTRatio { get; set; }
    public float AwayShotsPerMatch { get; set; }
    public float AwayShotsOnTargetPerMatch { get; set; }
    public float AwaySOTRatio { get; set; }

    public float HomeInjuryImpactScore { get; set; }
    public float HomeKeyPlayersInjured { get; set; }
    public float AwayInjuryImpactScore { get; set; }
    public float AwayKeyPlayersInjured { get; set; }
}
