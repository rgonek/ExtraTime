using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bots.Queries.GetBots;

public sealed record GetBotsQuery : IRequest<Result<List<BotDto>>>;
