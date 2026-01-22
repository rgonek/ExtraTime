using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public required string Token { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
