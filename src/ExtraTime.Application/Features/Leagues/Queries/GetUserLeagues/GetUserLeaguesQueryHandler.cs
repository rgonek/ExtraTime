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
            .Include(lm => lm.League)
                .ThenInclude(l => l.Owner)
            .Include(lm => lm.League)
                .ThenInclude(l => l.Members)
            .Select(lm => lm.League)
            .Distinct()
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = leagues.Select(l => new LeagueSummaryDto(
            Id: l.Id,
            Name: l.Name,
            OwnerUsername: l.Owner.Username,
            MemberCount: l.Members.Count,
            IsPublic: l.IsPublic,
            CreatedAt: l.CreatedAt))
            .ToList();

        return Result<List<LeagueSummaryDto>>.Success(result);
    }
}
