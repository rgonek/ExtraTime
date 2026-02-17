using ExtraTime.Domain.Entities;

namespace ExtraTime.Application.Common.Interfaces;

public interface ITeamUsualLineupService
{
    Task<TeamUsualLineup> GetOrCalculateAsync(
        Guid teamId,
        Guid seasonId,
        int matchesToAnalyze = 10,
        CancellationToken cancellationToken = default);
}
