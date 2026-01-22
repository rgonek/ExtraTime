using ExtraTime.Domain.Entities;

namespace ExtraTime.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    DateTime GetRefreshTokenExpiration();
}
