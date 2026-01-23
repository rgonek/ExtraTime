using System.Data;
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

        // Use Serializable isolation to prevent race conditions when multiple users join simultaneously
        return await context.ExecuteInTransactionAsync(async ct =>
        {
            var league = await context.Leagues
                .Include(l => l.Members)
                .FirstOrDefaultAsync(l => l.Id == request.LeagueId, ct);

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

            // Check if user is already a member
            var isAlreadyMember = league.Members.Any(m => m.UserId == userId);
            if (isAlreadyMember)
            {
                return Result.Failure(LeagueErrors.AlreadyAMember);
            }

            // Check if league is full (protected by transaction isolation)
            if (league.Members.Count >= league.MaxMembers)
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
            await context.SaveChangesAsync(ct);

            return Result.Success();
        }, IsolationLevel.Serializable, cancellationToken);
    }
}
