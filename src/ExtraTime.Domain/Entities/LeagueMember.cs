using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Entities;

public sealed class LeagueMember : BaseEntity
{
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public MemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
