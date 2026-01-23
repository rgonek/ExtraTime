using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Leagues.Commands.JoinLeague;

public sealed class JoinLeagueCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<JoinLeagueCommand, Result>
{
    public async ValueTask<Result> Handle(
        JoinLeagueCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        // Use a transaction with serializable isolation to prevent race conditions
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var league = await context.Leagues
                    .FirstOrDefaultAsync(l => l.Id == request.LeagueId, cancellationToken);

                if (league == null)
                {
                    return Result.Failure(LeagueErrors.LeagueNotFound);
                }

                // Validate invite code (case-insensitive)
                if (!league.InviteCode.Equals(request.InviteCode, StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Failure(LeagueErrors.InvalidInviteCode);
                }

                // Check if invite code is expired
                if (league.InviteCodeExpiresAt.HasValue && league.InviteCodeExpiresAt.Value < DateTime.UtcNow)
                {
                    return Result.Failure(LeagueErrors.InvalidInviteCode);
                }

                // Check if user is already a member (within transaction)
                var isAlreadyMember = await context.LeagueMembers
                    .AnyAsync(m => m.LeagueId == request.LeagueId && m.UserId == userId, cancellationToken);
                if (isAlreadyMember)
                {
                    return Result.Failure(LeagueErrors.AlreadyAMember);
                }

                // Count current members within the transaction to prevent race condition
                var currentMemberCount = await context.LeagueMembers
                    .CountAsync(m => m.LeagueId == request.LeagueId, cancellationToken);

                if (currentMemberCount >= league.MaxMembers)
                {
                    return Result.Failure(LeagueErrors.LeagueFull);
                }

                var member = new LeagueMember
                {
                    LeagueId = league.Id,
                    UserId = userId,
                    Role = MemberRole.Member,
                    JoinedAt = DateTime.UtcNow
                };

                context.LeagueMembers.Add(member);
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Result.Success();
            }
            catch (DbUpdateException)
            {
                // If unique constraint violation occurs (user already member),
                // return appropriate error
                return Result.Failure(LeagueErrors.AlreadyAMember);
            }
        });
    }
}
