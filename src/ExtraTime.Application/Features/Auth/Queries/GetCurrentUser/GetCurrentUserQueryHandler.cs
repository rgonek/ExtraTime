using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Auth.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    public async ValueTask<Result<UserDto>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
        {
            return Result<UserDto>.Failure(AuthErrors.UserNotFound);
        }

        var user = await context.Users
            .Where(u => u.Id == currentUserService.UserId)
            .Select(u => new UserDto(u.Id, u.Email, u.Username, u.Role))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.Failure(AuthErrors.UserNotFound);
        }

        return Result<UserDto>.Success(user);
    }
}
