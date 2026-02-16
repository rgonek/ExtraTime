using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

/// <summary>
/// Current injury status for a team sourced from API-Football.
/// </summary>
public sealed class TeamInjuries : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public int TotalInjured { get; set; }
    public int KeyPlayersInjured { get; set; }
    public int LongTermInjuries { get; set; }
    public int ShortTermInjuries { get; set; }
    public int Doubtful { get; set; }

    public string InjuredPlayerNames { get; set; } = string.Empty;
    public bool TopScorerInjured { get; set; }
    public bool CaptainInjured { get; set; }
    public bool FirstChoiceGkInjured { get; set; }

    public double InjuryImpactScore { get; set; }

    public DateTime LastSyncedAt { get; set; }
    public DateTime? NextSyncDue { get; set; }
}
