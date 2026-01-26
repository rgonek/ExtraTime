using ExtraTime.Domain.Common.Interfaces;

namespace ExtraTime.Domain.Common;

public static class Clock
{
    private static readonly AsyncLocal<IClock?> _current = new();
    private static readonly IClock _default = new SystemClock();

    public static IClock Current
    {
        get => _current.Value ?? _default;
        set => _current.Value = value;
    }

    public static DateTime UtcNow => Current.UtcNow;

    private sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
