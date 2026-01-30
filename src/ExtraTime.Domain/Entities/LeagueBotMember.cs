using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class LeagueBotMember : BaseEntity
{
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;

    public Guid BotId { get; set; }
    public Bot Bot { get; set; } = null!;

    public DateTime AddedAt { get; set; }
}
