namespace ExtraTime.Application.Common.Interfaces;

public interface IRefereeProfileService
{
    Task<RefereeProfileData?> GetRefereeProfileAsync(
        Guid matchId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}

public sealed record RefereeProfileData(
    string RefereeName,
    double CardsPerMatch,
    double FoulsPerMatch,
    double? PenaltiesPerMatch);
