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

        var league = await context.Leagues
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == request.LeagueId, cancellationToken);

        if (league == null)
        {
            return Result.Failure(LeagueErrors.LeagueNotFound);
        }

        try
        {
            league.RemoveMember(userId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
