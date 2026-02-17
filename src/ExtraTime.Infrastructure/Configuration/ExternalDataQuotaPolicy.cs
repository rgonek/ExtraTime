namespace ExtraTime.Infrastructure.Configuration;

public sealed class ExternalDataQuotaPolicy
{
    public int HardDailyLimit { get; set; } = 100;
    public int OperationalCap { get; set; } = 95;
    public int SafetyReserve { get; set; } = 10;
    public int MaxInjuryCallsPerDay { get; set; } = 15;
}
