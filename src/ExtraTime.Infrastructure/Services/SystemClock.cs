using ExtraTime.Domain.Common.Interfaces;

namespace ExtraTime.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
