namespace ExtraTime.Domain.Common.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}
