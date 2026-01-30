using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bots.Commands.AddBotToLeague;

public sealed class AddBotToLeagueCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<AddBotToLeagueCommand, Result<LeagueBotDto>>
{
    public async ValueTask<Result<LeagueBotDto>> Handle(AddBotToLeagueCommand request, CancellationToken ct)
    {
        var league = await context.Leagues
            .Include(l => l.BotMembers)
            .FirstOrDefaultAsync(l => l.Id == request.LeagueId, ct);

        if (league == null)
            return Result<LeagueBotDto>.Failure("League not found");

        if (league.OwnerId != currentUserService.UserId)
            return Result<LeagueBotDto>.Failure("Only the league owner can add bots");

        var bot = await context.Bots
            .FirstOrDefaultAsync(b => b.Id == request.BotId, ct);

        if (bot == null)
            return Result<LeagueBotDto>.Failure("Bot not found");

        if (league.BotMembers.Any(bm => bm.BotId == request.BotId))
            return Result<LeagueBotDto>.Failure("Bot is already in this league");

        league.AddBot(request.BotId);
        await context.SaveChangesAsync(ct);

        var dto = new LeagueBotDto(
            bot.Id,
            bot.Name,
            bot.AvatarUrl,
            bot.Strategy.ToString(),
            DateTime.UtcNow);

        return Result<LeagueBotDto>.Success(dto);
    }
}
