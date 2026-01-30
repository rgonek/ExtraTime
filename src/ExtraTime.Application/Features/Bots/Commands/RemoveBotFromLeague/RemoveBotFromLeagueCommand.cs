using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bots.Commands.RemoveBotFromLeague;

public sealed record RemoveBotFromLeagueCommand(
    Guid LeagueId,
    Guid BotId) : IRequest<Result>;
