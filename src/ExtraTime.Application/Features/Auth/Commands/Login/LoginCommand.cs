using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Auth.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password)
    : IRequest<Result<AuthResponse>>;
