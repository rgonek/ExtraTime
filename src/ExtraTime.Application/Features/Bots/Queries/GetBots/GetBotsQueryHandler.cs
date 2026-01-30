using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bots.Queries.GetBots;

public sealed class GetBotsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetBotsQuery, Result<List<BotDto>>>
{
    public async ValueTask<Result<List<BotDto>>> Handle(GetBotsQuery request, CancellationToken ct)
    {
        var bots = await context.Bots
            .Select(b => new BotDto(
                b.Id,
                b.Name,
                b.AvatarUrl,
                b.Strategy.ToString(),
                b.IsActive,
                b.CreatedAt,
                b.LastBetPlacedAt))
            .ToListAsync(ct);

        return Result<List<BotDto>>.Success(bots);
    }
}
