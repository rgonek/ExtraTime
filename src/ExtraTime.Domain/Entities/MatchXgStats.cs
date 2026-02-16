using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

/// <summary>
/// Expected goals statistics for a single match.
/// </summary>
public sealed class MatchXgStats : BaseEntity
{
    public required Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public double HomeXg { get; set; }
    public int HomeShots { get; set; }
    public int HomeShotsOnTarget { get; set; }

    public double AwayXg { get; set; }
    public int AwayShots { get; set; }
    public int AwayShotsOnTarget { get; set; }

    public bool HomeXgWin { get; set; }
    public bool ActualHomeWin { get; set; }
    public bool XgMatchedResult { get; set; }

    public int UnderstatMatchId { get; set; }
    public DateTime SyncedAt { get; set; }
}
