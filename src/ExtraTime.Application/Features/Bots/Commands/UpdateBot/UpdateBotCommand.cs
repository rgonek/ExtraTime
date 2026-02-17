using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Domain.Enums;
using Mediator;

namespace ExtraTime.Application.Features.Bots.Commands.UpdateBot;

public sealed record UpdateBotCommand(
    Guid BotId,
    string? Name,
    string? AvatarUrl,
    BotStrategy? Strategy,
    string? Configuration,
    bool? IsActive) : IRequest<Result<BotDto>>;
