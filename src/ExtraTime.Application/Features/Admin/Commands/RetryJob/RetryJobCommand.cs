using ExtraTime.Application.Common;
using Mediator;

namespace ExtraTime.Application.Features.Admin.Commands.RetryJob;

public sealed record RetryJobCommand(Guid JobId) : IRequest<Result>;
