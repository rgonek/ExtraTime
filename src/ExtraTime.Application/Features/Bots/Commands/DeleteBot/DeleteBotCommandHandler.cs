using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bots.Commands.DeleteBot;

public sealed class DeleteBotCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteBotCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteBotCommand request, CancellationToken ct)
    {
        var bot = await context.Bots
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == request.BotId, ct);

        if (bot is null)
        {
            return Result.Failure("Bot not found");
        }

        var hasBets = await context.Bets
            .AnyAsync(b => b.UserId == bot.UserId, ct);

        if (hasBets)
        {
            bot.Deactivate();
        }
        else
        {
            context.Bots.Remove(bot);
            context.Users.Remove(bot.User);
        }

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
