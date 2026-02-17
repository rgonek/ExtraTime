namespace ExtraTime.Application.Common.Interfaces;

public interface IExternalDataBackfillService
{
    Task BackfillForLeagueAsync(
        string leagueCode,
        int fromSeason,
        int toSeason,
        CancellationToken cancellationToken = default);

    Task BackfillGlobalEloAsync(
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default);
}
