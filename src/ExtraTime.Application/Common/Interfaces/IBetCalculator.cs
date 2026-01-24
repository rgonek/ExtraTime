using ExtraTime.Application.Features.Bets.DTOs;
using ExtraTime.Domain.Entities;

namespace ExtraTime.Application.Common.Interfaces;

public interface IBetCalculator
{
    BetResultDto CalculateResult(Bet bet, Match match, League league);
}
