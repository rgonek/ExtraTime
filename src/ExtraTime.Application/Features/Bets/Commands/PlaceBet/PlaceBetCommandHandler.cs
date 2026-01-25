using System.Text.Json;
using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets.DTOs;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bets.Commands.PlaceBet;

public sealed class PlaceBetCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<PlaceBetCommand, Result<BetDto>>
{
    public async ValueTask<Result<BetDto>> Handle(
        PlaceBetCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        // Check if league exists
        var league = await context.Leagues
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LeagueId, cancellationToken);

        if (league == null)
        {
            return Result<BetDto>.Failure(BetErrors.LeagueNotFound);
        }

        // Check if user is a member of the league
        var isMember = await context.LeagueMembers
            .AnyAsync(lm => lm.LeagueId == request.LeagueId && lm.UserId == userId, cancellationToken);

        if (!isMember)
        {
            return Result<BetDto>.Failure(BetErrors.NotALeagueMember);
        }

        // Check if match exists
        var match = await context.Matches
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);

        if (match == null)
        {
            return Result<BetDto>.Failure(BetErrors.MatchNotFound);
        }

        // Check if match is in allowed competitions (if league has competition restrictions)
        if (!string.IsNullOrEmpty(league.AllowedCompetitionIds))
        {
            var allowedIds = JsonSerializer.Deserialize<Guid[]>(league.AllowedCompetitionIds);
            if (allowedIds != null && !allowedIds.Contains(match.CompetitionId))
            {
                return Result<BetDto>.Failure(BetErrors.MatchNotAllowed);
            }
        }

        // Check match status - only allow betting on Scheduled or Timed matches
        if (match.Status != MatchStatus.Scheduled && match.Status != MatchStatus.Timed)
        {
            return Result<BetDto>.Failure(BetErrors.MatchAlreadyStarted);
        }

        // Check betting deadline
        var deadline = match.MatchDateUtc.AddMinutes(-league.BettingDeadlineMinutes);
        if (DateTime.UtcNow >= deadline)
        {
            return Result<BetDto>.Failure(BetErrors.DeadlinePassed);
        }

        // Check if user already has a bet for this match in this league (upsert behavior)
        var existingBet = await context.Bets
            .Include(b => b.Result)
            .FirstOrDefaultAsync(
                b => b.LeagueId == request.LeagueId &&
                     b.UserId == userId &&
                     b.MatchId == request.MatchId,
                cancellationToken);

        var now = DateTime.UtcNow;
        bool isNewBet = existingBet == null;

        if (existingBet == null)
        {
            // Create new bet
            existingBet = new Bet
            {
                LeagueId = request.LeagueId,
                UserId = userId,
                MatchId = request.MatchId,
                PredictedHomeScore = request.PredictedHomeScore,
                PredictedAwayScore = request.PredictedAwayScore,
                PlacedAt = now,
                LastUpdatedAt = null
            };
            context.Bets.Add(existingBet);
        }
        else
        {
            // Update existing bet
            existingBet.PredictedHomeScore = request.PredictedHomeScore;
            existingBet.PredictedAwayScore = request.PredictedAwayScore;
            existingBet.LastUpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);

        var resultDto = existingBet.Result != null
            ? new BetResultDto(
                existingBet.Result.PointsEarned,
                existingBet.Result.IsExactMatch,
                existingBet.Result.IsCorrectResult)
            : null;

        return Result<BetDto>.Success(new BetDto(
            Id: existingBet.Id,
            LeagueId: existingBet.LeagueId,
            UserId: existingBet.UserId,
            MatchId: existingBet.MatchId,
            PredictedHomeScore: existingBet.PredictedHomeScore,
            PredictedAwayScore: existingBet.PredictedAwayScore,
            PlacedAt: existingBet.PlacedAt,
            LastUpdatedAt: existingBet.LastUpdatedAt,
            Result: resultDto));
    }
}
