namespace ExtraTime.Application.Common.Interfaces;

public interface IBetResultsService
{
    Task<int> CalculateAllPendingBetResultsAsync(CancellationToken ct = default);
}
