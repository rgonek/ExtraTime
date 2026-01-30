using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bots.Commands.AddBotToLeague;

public sealed record AddBotToLeagueCommand(
    Guid LeagueId,
    Guid BotId) : IRequest<Result<LeagueBotDto>>;
