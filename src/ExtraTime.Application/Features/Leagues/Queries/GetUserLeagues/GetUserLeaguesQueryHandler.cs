using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Leagues.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Leagues.Queries.GetUserLeagues;

public sealed class GetUserLeaguesQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetUserLeaguesQuery, Result<List<LeagueSummaryDto>>>
{
    public async ValueTask<Result<List<LeagueSummaryDto>>> Handle(
        GetUserLeaguesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var leagues = await context.LeagueMembers
            .Where(lm => lm.UserId == userId)
            .Select(lm => new LeagueSummaryDto(
                Id: lm.League.Id,
                Name: lm.League.Name,
                OwnerUsername: lm.League.Owner.Username,
                MemberCount: lm.League.Members.Count,
                IsPublic: lm.League.IsPublic,
                CreatedAt: lm.League.CreatedAt))
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<List<LeagueSummaryDto>>.Success(leagues);
    }
}
