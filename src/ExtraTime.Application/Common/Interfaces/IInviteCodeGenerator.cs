namespace ExtraTime.Application.Common.Interfaces;

public interface IInviteCodeGenerator
{
    string Generate();

    Task<string> GenerateUniqueAsync(
        Func<string, CancellationToken, Task<bool>> existsCheck,
        CancellationToken cancellationToken = default);
}
