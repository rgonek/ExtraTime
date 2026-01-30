using ExtraTime.Domain.Entities;

namespace ExtraTime.Application.Common.Interfaces;

public interface ITeamFormCalculator
{
    Task<TeamFormCache> CalculateFormAsync(
        Guid teamId,
        Guid competitionId,
        int matchesAnalyzed = 5,
        CancellationToken cancellationToken = default);

    Task RefreshAllFormCachesAsync(CancellationToken cancellationToken = default);

    Task<TeamFormCache?> GetCachedFormAsync(
        Guid teamId,
        Guid competitionId,
        CancellationToken cancellationToken = default);
}
