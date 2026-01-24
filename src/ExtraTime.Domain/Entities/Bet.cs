using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class Bet : BaseAuditableEntity
{
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;

    // Prediction
    public int PredictedHomeScore { get; set; }
    public int PredictedAwayScore { get; set; }

    // Timestamps
    public DateTime PlacedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }

    // Navigation
    public BetResult? Result { get; set; }
}
