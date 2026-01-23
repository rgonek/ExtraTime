using System.Security.Cryptography;
using ExtraTime.Application.Common.Interfaces;

namespace ExtraTime.Infrastructure.Services;

public sealed class InviteCodeGenerator : IInviteCodeGenerator
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude ambiguous chars (I, O, 0, 1, L)
    private const int CodeLength = 8;

    public string Generate()
    {
        var result = new char[CodeLength];
        var randomBytes = new byte[CodeLength];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        for (var i = 0; i < CodeLength; i++)
        {
            result[i] = Chars[randomBytes[i] % Chars.Length];
        }

        return new string(result);
    }
}
