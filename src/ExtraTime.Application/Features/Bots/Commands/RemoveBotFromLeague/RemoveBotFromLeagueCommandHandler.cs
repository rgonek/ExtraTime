using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bots.Commands.RemoveBotFromLeague;

public sealed class RemoveBotFromLeagueCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<RemoveBotFromLeagueCommand, Result>
{
    public async ValueTask<Result> Handle(RemoveBotFromLeagueCommand request, CancellationToken ct)
    {
        var league = await context.Leagues
            .Include(l => l.BotMembers)
            .FirstOrDefaultAsync(l => l.Id == request.LeagueId, ct);

        if (league == null)
            return Result.Failure("League not found");

        if (league.OwnerId != currentUserService.UserId)
            return Result.Failure("Only the league owner can remove bots");

        var botMember = league.BotMembers.FirstOrDefault(bm => bm.BotId == request.BotId);
        if (botMember == null)
            return Result.Failure("Bot is not in this league");

        league.RemoveBot(request.BotId);
        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
