using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets.DTOs;
using ExtraTime.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bets.Queries.GetMatchBets;

public sealed class GetMatchBetsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetMatchBetsQuery, Result<List<MatchBetDto>>>
{
    public async ValueTask<Result<List<MatchBetDto>>> Handle(
        GetMatchBetsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        // Check if user is a member of the league
        var isMember = await context.LeagueMembers
            .AnyAsync(lm => lm.LeagueId == request.LeagueId && lm.UserId == userId, cancellationToken);

        if (!isMember)
        {
            return Result<List<MatchBetDto>>.Failure(BetErrors.NotALeagueMember);
        }

        // Get league and match to check deadline
        var league = await context.Leagues
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LeagueId, cancellationToken);

        if (league == null)
        {
            return Result<List<MatchBetDto>>.Failure(BetErrors.LeagueNotFound);
        }

        var match = await context.Matches
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);

        if (match == null)
        {
            return Result<List<MatchBetDto>>.Failure(BetErrors.MatchNotFound);
        }

        // Check if deadline has passed - if not, return empty list (bets are hidden)
        var deadline = match.MatchDateUtc.AddMinutes(-league.BettingDeadlineMinutes);
        if (Clock.UtcNow < deadline)
        {
            // Bets are intentionally hidden before the deadline
            return Result<List<MatchBetDto>>.Success([]);
        }

        // Deadline passed, return all bets for this match
        var bets = await context.Bets
            .AsNoTracking()
            .Include(b => b.User)
            .Include(b => b.Result)
            .Where(b => b.LeagueId == request.LeagueId && b.MatchId == request.MatchId)
            .Select(b => new MatchBetDto(
                UserId: b.UserId,
                Username: b.User.Username,
                PredictedHomeScore: b.PredictedHomeScore,
                PredictedAwayScore: b.PredictedAwayScore,
                Result: b.Result != null
                    ? new BetResultDto(
                        b.Result.PointsEarned,
                        b.Result.IsExactMatch,
                        b.Result.IsCorrectResult)
                    : null))
            .ToListAsync(cancellationToken);

        return Result<List<MatchBetDto>>.Success(bets);
    }
}
