namespace ExtraTime.Domain.ValueObjects;

public sealed record MatchScore(Score Home, Score Away)
{
    public override string ToString() => $"{Home}-{Away}";
}
