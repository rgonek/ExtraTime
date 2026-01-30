using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Bots.Services;
using Mediator;

namespace ExtraTime.Application.Features.Bots.Commands.PlaceBotBets;

public sealed record PlaceBotBetsCommand : IRequest<Result<int>>;
