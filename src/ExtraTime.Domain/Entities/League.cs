using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class League : BaseAuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Ownership
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    // Visibility & Size
    public bool IsPublic { get; set; } = false;
    public int MaxMembers { get; set; } = 255;

    // Scoring Rules
    public int ScoreExactMatch { get; set; } = 3;
    public int ScoreCorrectResult { get; set; } = 1;

    // Betting Rules
    public int BettingDeadlineMinutes { get; set; } = 5;

    // Competition Filter (null = all competitions allowed)
    public string? AllowedCompetitionIds { get; set; }  // JSON array of Guid[]

    // Invite System
    public required string InviteCode { get; set; }
    public DateTime? InviteCodeExpiresAt { get; set; }

    // Navigation
    public ICollection<LeagueMember> Members { get; set; } = [];
}
