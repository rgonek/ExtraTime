namespace ExtraTime.Application.Common.Interfaces;

public interface IFplInjuryStatusProvider
{
    Task<IReadOnlyList<FplPlayerInjuryStatus>> GetCurrentStatusesAsync(
        CancellationToken cancellationToken = default);
}

public sealed record FplPlayerInjuryStatus(
    int ExternalPlayerId,
    int FplTeamId,
    string PlayerName,
    string Status,
    string? News,
    DateTime? NewsUpdatedAtUtc);
