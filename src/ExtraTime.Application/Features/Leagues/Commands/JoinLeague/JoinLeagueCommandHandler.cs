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
            // PostgreSQL returns error code 23505 for unique_violation
            // Note: This code is PostgreSQL-specific. If changing database providers,
            // update this exception handling logic accordingly.
            var innerException = ex.InnerException;
            if (innerException != null)
            {
                var exceptionMessage = innerException.Message;
                // Check for unique constraint violation indicators
                // - "23505": PostgreSQL unique violation error code
                // - "duplicate key": Common message across databases for unique violations
                // - "league_members": Our table name to ensure it's the right constraint
                if ((exceptionMessage.Contains("23505") || 
                     exceptionMessage.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)) &&
                    exceptionMessage.Contains("league_members", StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Failure(LeagueErrors.AlreadyAMember);
                }
            }
            
            // Re-throw if it's a different database error
            throw;
        }
    }
}
