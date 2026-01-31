using ExtraTime.Domain.Common;
using ExtraTime.Domain.Events;

namespace ExtraTime.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? ReasonRevoked { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public bool IsExpired => Clock.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { } // Required for EF Core

    public static RefreshToken Create(
        string token,
        DateTime expiresAt,
        Guid userId,
        string? createdByIp = null)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required", nameof(token));

        if (expiresAt <= Clock.UtcNow)
            throw new ArgumentException("Expiration must be in the future", nameof(expiresAt));

        return new RefreshToken
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = userId,
            CreatedByIp = createdByIp,
            CreatedAt = Clock.UtcNow
        };
    }

    public void Revoke(string? revokedByIp = null, string? reason = null)
    {
        if (!IsActive)
            throw new InvalidOperationException("Token is already inactive");

        RevokedAt = Clock.UtcNow;
        RevokedByIp = revokedByIp;
        ReasonRevoked = reason;

        AddDomainEvent(new RefreshTokenRevoked(Id, UserId, reason));
    }

    public bool IsValidForUse()
    {
        return IsActive;
    }

    public RefreshToken ReplaceWith(string newToken, DateTime newExpiresAt, string? replacedByIp = null)
    {
        // Revoke current token
        Revoke(replacedByIp, "Replaced by new token");
        ReplacedByToken = newToken;

        // Create new token
        var replacement = Create(newToken, newExpiresAt, UserId, replacedByIp);
        replacement.AddDomainEvent(new RefreshTokenRotated(Id, replacement.Id, UserId));
        return replacement;
    }
}
