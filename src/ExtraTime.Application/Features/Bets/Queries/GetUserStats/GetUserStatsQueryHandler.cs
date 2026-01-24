using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bets.Queries.GetUserStats;

public sealed class GetUserStatsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetUserStatsQuery, Result<UserStatsDto>>
{
    public async ValueTask<Result<UserStatsDto>> Handle(
        GetUserStatsQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId!.Value;

        // Check if current user is a member of the league
        var isMember = await context.LeagueMembers
            .AnyAsync(lm => lm.LeagueId == request.LeagueId && lm.UserId == currentUserId, cancellationToken);

        if (!isMember)
        {
            return Result<UserStatsDto>.Failure(BetErrors.NotALeagueMember);
        }

        // Check if target user is a member of the league
        var isTargetMember = await context.LeagueMembers
            .AnyAsync(lm => lm.LeagueId == request.LeagueId && lm.UserId == request.UserId, cancellationToken);

        if (!isTargetMember)
        {
            return Result<UserStatsDto>.Failure(BetErrors.UserNotFound);
        }

        // Get user info
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return Result<UserStatsDto>.Failure(BetErrors.UserNotFound);
        }

        // Get standing
        var standing = await context.LeagueStandings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ls => ls.LeagueId == request.LeagueId && ls.UserId == request.UserId,
                cancellationToken);

        // Calculate rank
        var rank = 1;
        if (standing != null)
        {
            rank = await context.LeagueStandings
                .Where(ls => ls.LeagueId == request.LeagueId)
                .Join(
                    context.LeagueMembers.Where(lm => lm.LeagueId == request.LeagueId),
                    ls => ls.UserId,
                    lm => lm.UserId,
                    (ls, lm) => ls)
                .CountAsync(ls =>
                    ls.TotalPoints > standing.TotalPoints ||
                    (ls.TotalPoints == standing.TotalPoints && ls.ExactMatches > standing.ExactMatches) ||
                    (ls.TotalPoints == standing.TotalPoints && ls.ExactMatches == standing.ExactMatches && ls.BetsPlaced < standing.BetsPlaced) ||
                    (ls.TotalPoints == standing.TotalPoints && ls.ExactMatches == standing.ExactMatches && ls.BetsPlaced == standing.BetsPlaced && ls.UserId < standing.UserId),
                    cancellationToken) + 1;
        }

        // Calculate accuracy percentage
        var accuracyPercentage = standing?.BetsPlaced > 0
            ? (double)standing.CorrectResults / standing.BetsPlaced * 100
            : 0;

        return Result<UserStatsDto>.Success(new UserStatsDto(
            UserId: request.UserId,
            Username: user.Username,
            TotalPoints: standing?.TotalPoints ?? 0,
            BetsPlaced: standing?.BetsPlaced ?? 0,
            ExactMatches: standing?.ExactMatches ?? 0,
            CorrectResults: standing?.CorrectResults ?? 0,
            CurrentStreak: standing?.CurrentStreak ?? 0,
            BestStreak: standing?.BestStreak ?? 0,
            AccuracyPercentage: Math.Round(accuracyPercentage, 2),
            Rank: rank,
            LastUpdatedAt: standing?.LastUpdatedAt ?? DateTime.MinValue
        ));
    }
}
