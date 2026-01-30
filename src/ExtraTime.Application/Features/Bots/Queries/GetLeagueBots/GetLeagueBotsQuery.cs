using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bots.Queries.GetLeagueBots;

public sealed record GetLeagueBotsQuery(Guid LeagueId) : IRequest<Result<List<LeagueBotDto>>>;
