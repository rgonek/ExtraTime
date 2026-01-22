using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Auth.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(string Email, string Username, string Password)
    : IRequest<Result<AuthResponse>>;
