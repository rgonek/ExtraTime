using ExtraTime.Domain.Entities;

namespace ExtraTime.Application.Common.Interfaces;

public interface IEloRatingService
{
    Task SyncEloRatingsAsync(CancellationToken cancellationToken = default);

    Task SyncEloRatingsForDateAsync(
        DateTime dateUtc,
        CancellationToken cancellationToken = default);

    Task BackfillEloRatingsAsync(
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default);

    Task<TeamEloRating?> GetTeamEloAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);

    Task<TeamEloRating?> GetTeamEloAtDateAsync(
        Guid teamId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default);
}
