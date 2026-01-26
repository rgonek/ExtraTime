using ExtraTime.Domain.Common.Interfaces;

namespace ExtraTime.UnitTests.Helpers;

public sealed class FakeClock : IClock
{
    public FakeClock(DateTime initialTime)
    {
        UtcNow = initialTime;
    }

    public DateTime UtcNow { get; private set; }

    public void AdvanceBy(TimeSpan duration)
    {
        UtcNow = UtcNow.Add(duration);
    }

    public void SetTime(DateTime time)
    {
        UtcNow = time;
    }
}
