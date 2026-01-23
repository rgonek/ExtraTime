using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Leagues.Commands.DeleteLeague;

public sealed class DeleteLeagueCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteLeagueCommand, Result>
{
    public async ValueTask<Result> Handle(
        DeleteLeagueCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var league = await context.Leagues
            .FirstOrDefaultAsync(l => l.Id == request.LeagueId, cancellationToken);

        if (league == null)
        {
            return Result.Failure(LeagueErrors.LeagueNotFound);
        }

        // Check if user is the owner
        if (league.OwnerId != userId)
        {
            return Result.Failure(LeagueErrors.NotTheOwner);
        }

        context.Leagues.Remove(league);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
