namespace ExtraTime.Application.Features.Bets;

public static class BetErrors
{
    public const string NotALeagueMember = "You must be a member of this league to place bets";
    public const string LeagueNotFound = "League not found";
    public const string MatchNotFound = "Match not found";
    public const string BetNotFound = "Bet not found";
    public const string DeadlinePassed = "Betting deadline has passed for this match";
    public const string MatchAlreadyStarted = "Match has already started";
    public const string MatchNotAllowed = "This match is not allowed in this league";
    public const string InvalidScore = "Score predictions must be between 0 and 99";
    public const string NotBetOwner = "You can only modify your own bets";
    public const string StandingsNotFound = "Standings not found for this league";
    public const string UserNotFound = "User not found in this league";
}
