using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bets.Queries.GetMyBets;

public sealed class GetMyBetsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetMyBetsQuery, Result<List<MyBetDto>>>
{
    public async ValueTask<Result<List<MyBetDto>>> Handle(
        GetMyBetsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        // Check if user is a member of the league
        var isMember = await context.LeagueMembers
            .AnyAsync(lm => lm.LeagueId == request.LeagueId && lm.UserId == userId, cancellationToken);

        if (!isMember)
        {
            return Result<List<MyBetDto>>.Failure(BetErrors.NotALeagueMember);
        }

        var bets = await context.Bets
            .AsNoTracking()
            .Include(b => b.Match)
                .ThenInclude(m => m.HomeTeam)
            .Include(b => b.Match)
                .ThenInclude(m => m.AwayTeam)
            .Include(b => b.Result)
            .Where(b => b.LeagueId == request.LeagueId && b.UserId == userId)
            .OrderByDescending(b => b.Match.MatchDateUtc)
            .Select(b => new MyBetDto(
                BetId: b.Id,
                MatchId: b.MatchId,
                HomeTeamName: b.Match.HomeTeam.Name,
                AwayTeamName: b.Match.AwayTeam.Name,
                MatchDateUtc: b.Match.MatchDateUtc,
                MatchStatus: b.Match.Status,
                ActualHomeScore: b.Match.HomeScore,
                ActualAwayScore: b.Match.AwayScore,
                PredictedHomeScore: b.PredictedHomeScore,
                PredictedAwayScore: b.PredictedAwayScore,
                Result: b.Result != null
                    ? new BetResultDto(
                        b.Result.PointsEarned,
                        b.Result.IsExactMatch,
                        b.Result.IsCorrectResult)
                    : null,
                PlacedAt: b.PlacedAt))
            .ToListAsync(cancellationToken);

        return Result<List<MyBetDto>>.Success(bets);
    }
}
