using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bots.Queries.GetBots;

public sealed class GetBotsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetBotsQuery, Result<List<BotDto>>>
{
    public async ValueTask<Result<List<BotDto>>> Handle(GetBotsQuery request, CancellationToken ct)
    {
        IQueryable<Bot> query = context.Bots;

        if (request.IncludeInactive != true)
        {
            query = query.Where(b => b.IsActive);
        }

        if (request.Strategy.HasValue)
        {
            query = query.Where(b => b.Strategy == request.Strategy.Value);
        }

        var bots = await query
            .OrderBy(b => b.Name)
            .ToListAsync(ct);

        var botDtos = new List<BotDto>(bots.Count);
        foreach (var bot in bots)
        {
            var stats = await GetBotStatsAsync(bot.Id, bot.UserId, ct);

            botDtos.Add(new BotDto(
                bot.Id,
                bot.Name,
                bot.AvatarUrl,
                bot.Strategy.ToString(),
                bot.IsActive,
                bot.CreatedAt,
                bot.LastBetPlacedAt,
                bot.Configuration,
                stats));
        }

        return Result<List<BotDto>>.Success(botDtos);
    }

    private async Task<BotStatsDto> GetBotStatsAsync(Guid botId, Guid userId, CancellationToken ct)
    {
        var bets = await context.Bets
            .Include(b => b.Result)
            .Where(b => b.UserId == userId)
            .ToListAsync(ct);

        var leaguesJoined = await context.LeagueBotMembers
            .CountAsync(lbm => lbm.BotId == botId, ct);

        var betsWithResults = bets.Where(b => b.Result is not null).ToList();

        return new BotStatsDto(
            TotalBetsPlaced: bets.Count,
            LeaguesJoined: leaguesJoined,
            AveragePointsPerBet: betsWithResults.Count > 0
                ? betsWithResults.Average(b => b.Result!.PointsEarned)
                : 0,
            ExactPredictions: betsWithResults.Count(b => b.Result!.IsExactMatch),
            CorrectResults: betsWithResults.Count(b => b.Result!.IsCorrectResult));
    }
}
