using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bets.Commands.DeleteBet;

public sealed class DeleteBetCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteBetCommand, Result>
{
    public async ValueTask<Result> Handle(
        DeleteBetCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        // Check if bet exists
        var bet = await context.Bets
            .Include(b => b.Match)
            .FirstOrDefaultAsync(b => b.Id == request.BetId && b.LeagueId == request.LeagueId, cancellationToken);

        if (bet == null)
        {
            return Result.Failure(BetErrors.BetNotFound);
        }

        // Check if user owns the bet
        if (bet.UserId != userId)
        {
            return Result.Failure(BetErrors.NotBetOwner);
        }

        // Check match status - only allow deletion on Scheduled or Timed matches
        if (bet.Match.Status != MatchStatus.Scheduled && bet.Match.Status != MatchStatus.Timed)
        {
            return Result.Failure(BetErrors.MatchAlreadyStarted);
        }

        // Check betting deadline
        var league = await context.Leagues
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LeagueId, cancellationToken);

        if (league == null)
        {
            return Result.Failure(BetErrors.LeagueNotFound);
        }

        var deadline = bet.Match.MatchDateUtc.AddMinutes(-league.BettingDeadlineMinutes);
        if (DateTime.UtcNow >= deadline)
        {
            return Result.Failure(BetErrors.DeadlinePassed);
        }

        context.Bets.Remove(bet);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
