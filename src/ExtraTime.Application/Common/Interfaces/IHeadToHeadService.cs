using ExtraTime.Domain.Entities;

namespace ExtraTime.Application.Common.Interfaces;

public interface IHeadToHeadService
{
    Task<HeadToHead> GetOrCalculateAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default);

    Task<HeadToHead> RefreshAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default);
}
