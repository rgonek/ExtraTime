namespace ExtraTime.Application.Features.Bots.Services;

public interface IBotBettingService
{
    Task<int> PlaceBetsForUpcomingMatchesAsync(CancellationToken cancellationToken = default);
    Task<int> PlaceBetsForLeagueAsync(Guid leagueId, CancellationToken cancellationToken = default);
}
