using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bots.Commands.UpdateBot;

public sealed class UpdateBotCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateBotCommand, Result<BotDto>>
{
    public async ValueTask<Result<BotDto>> Handle(UpdateBotCommand request, CancellationToken ct)
    {
        var bot = await context.Bots
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == request.BotId, ct);

        if (bot is null)
        {
            return Result<BotDto>.Failure("Bot not found");
        }

        var updatedName = bot.Name;
        var updatedAvatarUrl = bot.AvatarUrl;
        var detailsChanged = false;

        if (!string.IsNullOrWhiteSpace(request.Name) &&
            !string.Equals(request.Name, bot.Name, StringComparison.Ordinal))
        {
            var existingBot = await context.Bots
                .FirstOrDefaultAsync(b => b.Name == request.Name && b.Id != request.BotId, ct);

            if (existingBot is not null)
            {
                return Result<BotDto>.Failure($"Bot with name '{request.Name}' already exists");
            }

            updatedName = request.Name;
            detailsChanged = true;
        }

        if (request.AvatarUrl is not null &&
            !string.Equals(request.AvatarUrl, bot.AvatarUrl, StringComparison.Ordinal))
        {
            updatedAvatarUrl = request.AvatarUrl;
            detailsChanged = true;
        }

        if (detailsChanged)
        {
            bot.UpdateDetails(updatedName, updatedAvatarUrl);
            bot.User.UpdateProfile(bot.User.Email, updatedName);
        }

        if (request.Strategy.HasValue)
        {
            bot.UpdateStrategy(request.Strategy.Value);
        }

        if (request.Configuration is not null)
        {
            bot.UpdateConfiguration(request.Configuration);
        }

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
            {
                bot.Activate();
            }
            else
            {
                bot.Deactivate();
            }
        }

        await context.SaveChangesAsync(ct);

        var dto = new BotDto(
            bot.Id,
            bot.Name,
            bot.AvatarUrl,
            bot.Strategy.ToString(),
            bot.IsActive,
            bot.CreatedAt,
            bot.LastBetPlacedAt,
            bot.Configuration);

        return Result<BotDto>.Success(dto);
    }
}
