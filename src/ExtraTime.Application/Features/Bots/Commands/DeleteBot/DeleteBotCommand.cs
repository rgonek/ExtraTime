using ExtraTime.Application.Common;
using Mediator;

namespace ExtraTime.Application.Features.Bots.Commands.DeleteBot;

public sealed record DeleteBotCommand(Guid BotId) : IRequest<Result>;
