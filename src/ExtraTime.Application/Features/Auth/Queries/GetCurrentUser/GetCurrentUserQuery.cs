using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Auth.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<Result<UserDto>>;
