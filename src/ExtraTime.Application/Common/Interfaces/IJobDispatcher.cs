using ExtraTime.Domain.Entities;

namespace ExtraTime.Application.Common.Interfaces;

public interface IJobDispatcher
{
    Task<Guid> EnqueueAsync<T>(string jobType, T payload, CancellationToken cancellationToken = default);
    Task DispatchAsync(BackgroundJob job, CancellationToken cancellationToken = default);
}
