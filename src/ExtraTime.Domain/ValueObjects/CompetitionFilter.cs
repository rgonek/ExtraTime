namespace ExtraTime.Domain.ValueObjects;

public sealed record CompetitionFilter(IReadOnlyCollection<Guid> AllowedIds)
{
    public bool IsAllowed(Guid competitionId)
        => !AllowedIds.Any() || AllowedIds.Contains(competitionId);

    public static CompetitionFilter All => new(Array.Empty<Guid>());
}
