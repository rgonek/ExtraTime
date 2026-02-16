using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

/// <summary>
/// Daily Elo rating snapshot for a team from ClubElo.
/// </summary>
public sealed class TeamEloRating : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public double EloRating { get; set; }
    public int EloRank { get; set; }
    public string ClubEloName { get; set; } = string.Empty;

    public DateTime RatingDate { get; set; }
    public DateTime SyncedAt { get; set; }
}
