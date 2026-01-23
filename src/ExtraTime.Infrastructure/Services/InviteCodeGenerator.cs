using System.Security.Cryptography;
using ExtraTime.Application.Common.Interfaces;

namespace ExtraTime.Infrastructure.Services;

public sealed class InviteCodeGenerator : IInviteCodeGenerator
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude ambiguous chars (I, O, 0, 1, L)
    private const int CodeLength = 8;
    private const int MaxAttempts = 10;

    public string Generate()
    {
        var result = new char[CodeLength];

        for (var i = 0; i < CodeLength; i++)
        {
            result[i] = Chars[RandomNumberGenerator.GetInt32(Chars.Length)];
        }

        return new string(result);
    }

    public async Task<string> GenerateUniqueAsync(
        Func<string, CancellationToken, Task<bool>> existsCheck,
        CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < MaxAttempts; i++)
        {
            var code = Generate();
            var exists = await existsCheck(code, cancellationToken);

            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Failed to generate unique invite code after multiple attempts");
    }
}
