using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.Events;
using ExtraTime.Domain.ValueObjects;

namespace ExtraTime.Domain.Entities;

public sealed class User : BaseAuditableEntity
{
    private readonly List<RefreshToken> _refreshTokens = [];

    public string Email { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsBot { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public Bot? Bot { get; set; }

    private User() { } // Required for EF Core

    public static User Register(string email, string username, string passwordHash, UserRole role = UserRole.User)
    {
        // Validation via value objects
        var emailVo = new Email(email);
        var usernameVo = new Username(username);

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required", nameof(passwordHash));

        var user = new User
        {
            Email = emailVo.Value,
            Username = usernameVo.Value,
            PasswordHash = passwordHash,
            Role = role
        };

        user.AddDomainEvent(new UserRegistered(user.Id, user.Email));
        return user;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = Clock.UtcNow;
        AddDomainEvent(new UserLoggedIn(Id));
    }

    public void AddRefreshToken(string token, DateTime expiresAt, string? createdByIp = null)
    {
        // Remove expired and old revoked tokens
        _refreshTokens.RemoveAll(t => t.IsExpired || (t.IsRevoked && t.RevokedAt?.AddDays(7) < Clock.UtcNow));

        var refreshToken = new RefreshToken
        {
            Token = token,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp,
            UserId = Id,
            CreatedAt = Clock.UtcNow
        };

        _refreshTokens.Add(refreshToken);
    }

    public void RevokeRefreshToken(string token, string? revokedByIp = null, string? reason = null)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(t => t.Token == token);
        if (refreshToken != null && refreshToken.IsActive)
        {
            refreshToken.RevokedAt = Clock.UtcNow;
            refreshToken.RevokedByIp = revokedByIp;
            refreshToken.ReasonRevoked = reason;
        }
    }

    public void UpdateProfile(string email, string username)
    {
        Email = new Email(email).Value;
        Username = new Username(username).Value;
    }

    public void MarkAsBot()
    {
        IsBot = true;
    }
}
