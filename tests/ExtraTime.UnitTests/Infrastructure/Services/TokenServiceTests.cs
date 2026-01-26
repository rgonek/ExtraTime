using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Services;
using ExtraTime.UnitTests.TestData;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;

namespace ExtraTime.UnitTests.Infrastructure.Services;

public sealed class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public TokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            Secret = "a_very_long_secret_key_for_testing_purposes_only",
            Issuer = "ExtraTime",
            Audience = "ExtraTimeUsers",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        var options = Substitute.For<IOptions<JwtSettings>>();
        options.Value.Returns(_jwtSettings);

        _tokenService = new TokenService(options);
    }

    [Test]
    public async Task GenerateAccessToken_ReturnsValidJwt()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@example.com")
            .WithUsername("testuser")
            .WithRole(UserRole.Admin)
            .Build();

        // Act
        var token = _tokenService.GenerateAccessToken(user);

        // Assert
        await Assert.That(token).IsNotNull();
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        await Assert.That(jwtToken.Issuer).IsEqualTo(_jwtSettings.Issuer);
        await Assert.That(jwtToken.Audiences.First()).IsEqualTo(_jwtSettings.Audience);
        await Assert.That(jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value).IsEqualTo(user.Id.ToString());
        await Assert.That(jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value).IsEqualTo(user.Email);
    }

    [Test]
    public async Task GenerateRefreshToken_ReturnsRandomString()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        await Assert.That(token1).IsNotNull();
        await Assert.That(token2).IsNotNull();
        await Assert.That(token1).IsNotEqualTo(token2);
        await Assert.That(token1.Length).IsGreaterThan(10);
    }

    [Test]
    public async Task GetRefreshTokenExpiration_ReturnsFutureDate()
    {
        // Act
        var expiration = _tokenService.GetRefreshTokenExpiration();

        // Assert
        await Assert.That(expiration).IsGreaterThan(DateTime.UtcNow);
        await Assert.That(expiration).IsLessThan(DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays + 1));
    }
}
