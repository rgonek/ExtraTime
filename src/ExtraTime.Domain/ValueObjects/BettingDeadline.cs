namespace ExtraTime.Domain.ValueObjects;

public sealed record BettingDeadline
{
    public int MinutesBeforeMatch { get; init; }

    public BettingDeadline(int minutesBeforeMatch)
    {
        if (minutesBeforeMatch < 0)
            throw new ArgumentException("Betting deadline cannot be negative", nameof(minutesBeforeMatch));

        MinutesBeforeMatch = minutesBeforeMatch;
    }

    public bool IsMatchOpen(DateTime matchStartTime, DateTime currentTime)
    {
        return currentTime <= matchStartTime.AddMinutes(-MinutesBeforeMatch);
    }

    public static implicit operator int(BettingDeadline deadline) => deadline.MinutesBeforeMatch;
    public static explicit operator BettingDeadline(int minutes) => new(minutes);

    public override string ToString() => $"{MinutesBeforeMatch} minutes";
}
