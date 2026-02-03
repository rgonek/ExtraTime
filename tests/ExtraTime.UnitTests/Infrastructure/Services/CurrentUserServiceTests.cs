using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Services;
using ExtraTime.UnitTests.Attributes;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

[TestCategory("Significant")]
public sealed class CurrentUserServiceTests
{
    private IHttpContextAccessor _httpContextAccessor = null!;
    private CurrentUserService _service = null!;
    private DefaultHttpContext _httpContext = null!;

    [Before(Test)]
    public void Setup()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _httpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(_httpContext);
        _service = new CurrentUserService(_httpContextAccessor);
    }

    [Test]
    public async Task UserId_Authenticated_ReturnsUserId()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, expectedUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _service.UserId;

        // Assert
        await Assert.That(result).IsEqualTo(expectedUserId);
    }

    [Test]
    public async Task UserId_NotAuthenticated_ReturnsNull()
    {
        // Arrange
        _httpContext.User = new ClaimsPrincipal();

        // Act
        var result = _service.UserId;

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task IsAdmin_AdminRole_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, nameof(UserRole.Admin))
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _service.IsAdmin;

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAdmin_UserRole_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, nameof(UserRole.User))
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _service.IsAdmin;

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAuthenticated_Authenticated_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _service.IsAuthenticated;

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task UserId_FallsBackToNameIdentifier_WhenSubNotPresent()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, expectedUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _service.UserId;

        // Assert
        await Assert.That(result).IsEqualTo(expectedUserId);
    }

    [Test]
    public async Task UserId_InvalidGuid_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "not-a-valid-guid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _service.UserId;

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Role_ReturnsRoleClaim()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _service.Role;

        // Assert
        await Assert.That(result).IsEqualTo("Admin");
    }

    [Test]
    public async Task Role_NoRoleClaim_ReturnsNull()
    {
        // Arrange
        var identity = new ClaimsIdentity([], "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _service.Role;

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task IsAuthenticated_NotAuthenticated_ReturnsFalse()
    {
        // Arrange
        _httpContext.User = new ClaimsPrincipal();

        // Act
        var result = _service.IsAuthenticated;

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAdmin_NoRole_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity([], "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _service.IsAdmin;

        // Assert
        await Assert.That(result).IsFalse();
    }
}
