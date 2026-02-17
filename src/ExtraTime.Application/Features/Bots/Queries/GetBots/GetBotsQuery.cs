using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Domain.Enums;
using Mediator;

namespace ExtraTime.Application.Features.Bots.Queries.GetBots;

public sealed record GetBotsQuery(
    bool? IncludeInactive = false,
    BotStrategy? Strategy = null) : IRequest<Result<List<BotDto>>>;
