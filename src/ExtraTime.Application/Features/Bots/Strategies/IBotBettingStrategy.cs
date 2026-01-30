using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Application.Features.Bots.Strategies;

public interface IBotBettingStrategy
{
    BotStrategy StrategyType { get; }
    (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration);
}
