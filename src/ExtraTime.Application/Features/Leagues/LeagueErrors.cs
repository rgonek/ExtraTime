namespace ExtraTime.Application.Features.Leagues;

public static class LeagueErrors
{
    public const string LeagueNotFound = "League not found";
    public const string NotAMember = "You are not a member of this league";
    public const string NotTheOwner = "Only the league owner can perform this action";
    public const string InvalidInviteCode = "Invalid or expired invite code";
    public const string LeagueFull = "This league is full";
    public const string AlreadyAMember = "You are already a member of this league";
    public const string OwnerCannotLeave = "League owner cannot leave. Delete the league instead";
    public const string CannotKickOwner = "Cannot kick the league owner";
    public const string MemberNotFound = "Member not found in this league";
}
