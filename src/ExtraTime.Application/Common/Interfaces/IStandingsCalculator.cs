namespace ExtraTime.Application.Common.Interfaces;

public interface IStandingsCalculator
{
    Task RecalculateLeagueStandingsAsync(Guid leagueId, CancellationToken cancellationToken = default);
}
