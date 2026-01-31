using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Leagues.Commands.KickMember;

public sealed class KickMemberCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<KickMemberCommand, Result>
{
    public async ValueTask<Result> Handle(
        KickMemberCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var league = await context.Leagues
            .FirstOrDefaultAsync(l => l.Id == request.LeagueId, cancellationToken);

        if (league == null)
        {
            return Result.Failure(LeagueErrors.LeagueNotFound);
        }

        // Check if current user is the owner
        if (league.OwnerId != userId)
        {
            return Result.Failure(LeagueErrors.NotTheOwner);
        }

        var targetMember = await context.LeagueMembers
            .FirstOrDefaultAsync(lm => lm.LeagueId == request.LeagueId && lm.UserId == request.UserId, cancellationToken);

        if (targetMember == null)
        {
            return Result.Failure(LeagueErrors.MemberNotFound);
        }

        // Cannot kick the owner - using rich domain method
        if (targetMember.IsOwner())
        {
            return Result.Failure(LeagueErrors.CannotKickOwner);
        }

        context.LeagueMembers.Remove(targetMember);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
