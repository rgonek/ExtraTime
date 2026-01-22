using ExtraTime.Application.Common;
using Mediator;

namespace ExtraTime.Application.Features.Admin.Commands.CancelJob;

public sealed record CancelJobCommand(Guid JobId) : IRequest<Result>;
