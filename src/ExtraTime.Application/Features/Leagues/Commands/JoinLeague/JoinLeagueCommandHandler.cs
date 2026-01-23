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

        // Use a transaction to prevent race conditions on member count check
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
            catch (DbUpdateException ex)
            {
                // Check if it's a unique constraint violation (duplicate LeagueId + UserId)
                // PostgreSQL: error code 23505 (unique_violation)
                // The unique constraint is on (LeagueId, UserId) pair
                var innerException = ex.InnerException;
                if (innerException != null)
                {
                    var exceptionMessage = innerException.Message;
                    // Check for PostgreSQL unique constraint violation or general unique constraint error
                    if (exceptionMessage.Contains("23505") || // PostgreSQL unique violation error code
                        exceptionMessage.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                        exceptionMessage.Contains("IX_league_members_LeagueId_UserId", StringComparison.OrdinalIgnoreCase))
                    {
                        return Result.Failure(LeagueErrors.AlreadyAMember);
                    }
                }
                
                // Re-throw if it's a different database error
                throw;
            }
        });
    }
}
