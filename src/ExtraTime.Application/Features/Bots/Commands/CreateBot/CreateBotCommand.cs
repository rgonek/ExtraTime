using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Mediator;

namespace ExtraTime.Application.Features.Bots.Commands.CreateBot;

public sealed record CreateBotCommand(
    string Name,
    string? AvatarUrl,
    BotStrategy Strategy) : IRequest<Result<BotDto>>;
