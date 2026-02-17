using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class MatchStats : BaseEntity
{
    public required Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public int? HomeShots { get; set; }
    public int? HomeShotsOnTarget { get; set; }
    public int? AwayShots { get; set; }
    public int? AwayShotsOnTarget { get; set; }

    public int? HomeHalfTimeGoals { get; set; }
    public int? AwayHalfTimeGoals { get; set; }

    public int? HomeCorners { get; set; }
    public int? AwayCorners { get; set; }
    public int? HomeFouls { get; set; }
    public int? AwayFouls { get; set; }
    public int? HomeYellowCards { get; set; }
    public int? AwayYellowCards { get; set; }
    public int? HomeRedCards { get; set; }
    public int? AwayRedCards { get; set; }

    public string? Referee { get; set; }

    public string DataSource { get; set; } = "football-data.co.uk";
    public DateTime ImportedAt { get; set; }
}
