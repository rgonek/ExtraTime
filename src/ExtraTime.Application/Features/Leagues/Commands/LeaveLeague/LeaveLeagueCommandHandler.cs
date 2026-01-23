using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Leagues.Commands.LeaveLeague;

public sealed class LeaveLeagueCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<LeaveLeagueCommand, Result>
{
    public async ValueTask<Result> Handle(
        LeaveLeagueCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var member = await context.LeagueMembers
            .FirstOrDefaultAsync(lm => lm.LeagueId == request.LeagueId && lm.UserId == userId, cancellationToken);

        if (member == null)
        {
            return Result.Failure(LeagueErrors.NotAMember);
        }

        // Check if user is the owner
        if (member.Role == MemberRole.Owner)
        {
            return Result.Failure(LeagueErrors.OwnerCannotLeave);
        }

        context.LeagueMembers.Remove(member);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
