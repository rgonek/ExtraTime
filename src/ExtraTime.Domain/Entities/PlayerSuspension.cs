using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class PlayerSuspension : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public int ExternalPlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool IsKeyPlayer { get; set; }

    public string SuspensionReason { get; set; } = string.Empty;
    public DateTime? ExpectedReturn { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime LastUpdatedAt { get; set; }
}
