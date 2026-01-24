using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bets.Queries.GetLeagueStandings;

public sealed class GetLeagueStandingsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetLeagueStandingsQuery, Result<List<LeagueStandingDto>>>
{
    public async ValueTask<Result<List<LeagueStandingDto>>> Handle(
        GetLeagueStandingsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        // Check if user is a member of the league
        var isMember = await context.LeagueMembers
            .AnyAsync(lm => lm.LeagueId == request.LeagueId && lm.UserId == userId, cancellationToken);

        if (!isMember)
        {
            return Result<List<LeagueStandingDto>>.Failure(BetErrors.NotALeagueMember);
        }

        // Get standings only for current league members
        // Join with LeagueMembers to filter out kicked users
        var standings = await context.LeagueStandings
            .AsNoTracking()
            .Where(ls => ls.LeagueId == request.LeagueId)
            .Join(
                context.LeagueMembers.Where(lm => lm.LeagueId == request.LeagueId),
                ls => ls.UserId,
                lm => lm.UserId,
                (ls, lm) => new { Standing = ls, Member = lm })
            .Join(
                context.Users,
                x => x.Standing.UserId,
                u => u.Id,
                (x, u) => new
                {
                    x.Standing,
                    User = u
                })
            .OrderByDescending(x => x.Standing.TotalPoints)
            .ThenByDescending(x => x.Standing.ExactMatches)
            .ThenBy(x => x.Standing.BetsPlaced)
            .ThenBy(x => x.Standing.UserId)
            .ToListAsync(cancellationToken);

        // Assign ranks
        var result = standings.Select((x, index) => new LeagueStandingDto(
            UserId: x.Standing.UserId,
            Username: x.User.Username,
            Email: x.User.Email,
            Rank: index + 1,
            TotalPoints: x.Standing.TotalPoints,
            BetsPlaced: x.Standing.BetsPlaced,
            ExactMatches: x.Standing.ExactMatches,
            CorrectResults: x.Standing.CorrectResults,
            CurrentStreak: x.Standing.CurrentStreak,
            BestStreak: x.Standing.BestStreak,
            LastUpdatedAt: x.Standing.LastUpdatedAt
        )).ToList();

        return Result<List<LeagueStandingDto>>.Success(result);
    }
}
