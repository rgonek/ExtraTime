using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class CompetitionTeam : BaseEntity
{
    public Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public int Season { get; set; }
}
