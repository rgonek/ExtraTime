using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bots.Queries.GetLeagueBots;

public sealed class GetLeagueBotsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetLeagueBotsQuery, Result<List<LeagueBotDto>>>
{
    public async ValueTask<Result<List<LeagueBotDto>>> Handle(GetLeagueBotsQuery request, CancellationToken ct)
    {
        var bots = await context.LeagueBotMembers
            .Where(lbm => lbm.LeagueId == request.LeagueId)
            .Include(lbm => lbm.Bot)
            .Select(lbm => new LeagueBotDto(
                lbm.Bot.Id,
                lbm.Bot.Name,
                lbm.Bot.AvatarUrl,
                lbm.Bot.Strategy.ToString(),
                lbm.AddedAt))
            .ToListAsync(ct);

        return Result<List<LeagueBotDto>>.Success(bots);
    }
}
