using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bots.Commands.CreateBot;

public sealed class CreateBotCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher) : IRequestHandler<CreateBotCommand, Result<BotDto>>
{
    public async ValueTask<Result<BotDto>> Handle(CreateBotCommand request, CancellationToken ct)
    {
        var existingBot = await context.Bots
            .FirstOrDefaultAsync(b => b.Name == request.Name, ct);

        if (existingBot != null)
            return Result<BotDto>.Failure("A bot with this name already exists");

        var email = $"bot_{request.Name.ToLower().Replace(" ", "_")}@extratime.local";
        var passwordHash = passwordHasher.Hash(Guid.NewGuid().ToString());

        var user = User.Register(email, request.Name, passwordHash, UserRole.User);
        user.MarkAsBot();

        var bot = new Bot
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = request.Name,
            AvatarUrl = request.AvatarUrl,
            Strategy = request.Strategy,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Bots.Add(bot);
        await context.SaveChangesAsync(ct);

        var dto = new BotDto(
            bot.Id,
            bot.Name,
            bot.AvatarUrl,
            bot.Strategy.ToString(),
            bot.IsActive,
            bot.CreatedAt,
            bot.LastBetPlacedAt);

        return Result<BotDto>.Success(dto);
    }
}
