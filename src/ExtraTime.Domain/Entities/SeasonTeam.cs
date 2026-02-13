using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class SeasonTeam : BaseEntity
{
    public Guid SeasonId { get; private set; }
    public Guid TeamId { get; private set; }

    public Season Season { get; private set; } = null!;
    public Team Team { get; private set; } = null!;

    private SeasonTeam() { }

    public static SeasonTeam Create(Guid seasonId, Guid teamId)
    {
        return new SeasonTeam
        {
            SeasonId = seasonId,
            TeamId = teamId
        };
    }
}
