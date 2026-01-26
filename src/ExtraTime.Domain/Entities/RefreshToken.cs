using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public required string Token { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = Clock.UtcNow;
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? ReasonRevoked { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsExpired => Clock.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
