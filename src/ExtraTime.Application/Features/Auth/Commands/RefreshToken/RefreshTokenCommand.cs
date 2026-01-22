using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Auth.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken)
    : IRequest<Result<AuthResponse>>;
