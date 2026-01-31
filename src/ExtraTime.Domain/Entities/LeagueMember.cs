using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Entities;

public sealed class LeagueMember : BaseEntity
{
    public Guid LeagueId { get; private set; }
    public League League { get; private set; } = null!;

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public MemberRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private LeagueMember() { } // Required for EF Core

    public static LeagueMember Create(Guid leagueId, Guid userId, MemberRole role)
    {
        return new LeagueMember
        {
            LeagueId = leagueId,
            UserId = userId,
            Role = role,
            JoinedAt = Clock.UtcNow
        };
    }

    public void ChangeRole(MemberRole newRole)
    {
        if (Role == MemberRole.Owner && newRole != MemberRole.Owner)
            throw new InvalidOperationException("Cannot demote owner directly. Transfer ownership first.");

        if (Role == newRole) return;

        Role = newRole;
    }

    public bool IsOwner() => Role == MemberRole.Owner;
}
