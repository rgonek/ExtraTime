using System.Text.Json;
using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Leagues.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Leagues.Commands.RegenerateInviteCode;

public sealed class RegenerateInviteCodeCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IInviteCodeGenerator inviteCodeGenerator) : IRequestHandler<RegenerateInviteCodeCommand, Result<LeagueDto>>
{
    public async ValueTask<Result<LeagueDto>> Handle(
        RegenerateInviteCodeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var league = await context.Leagues
            .Include(l => l.Owner)
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == request.LeagueId, cancellationToken);

        if (league == null)
        {
            return Result<LeagueDto>.Failure(LeagueErrors.LeagueNotFound);
        }

        // Check if user is the owner
        if (league.OwnerId != userId)
        {
            return Result<LeagueDto>.Failure(LeagueErrors.NotTheOwner);
        }

        // Generate new unique invite code
        var newInviteCode = await GenerateUniqueInviteCodeAsync(cancellationToken);

        league.InviteCode = newInviteCode;
        league.InviteCodeExpiresAt = request.ExpiresAt;
        league.UpdatedAt = DateTime.UtcNow;
        league.UpdatedBy = userId.ToString();

        await context.SaveChangesAsync(cancellationToken);

        // Parse AllowedCompetitionIds for response
        Guid[]? allowedCompetitionIds = null;
        if (!string.IsNullOrEmpty(league.AllowedCompetitionIds))
        {
            allowedCompetitionIds = JsonSerializer.Deserialize<Guid[]>(league.AllowedCompetitionIds);
        }

        return Result<LeagueDto>.Success(new LeagueDto(
            Id: league.Id,
            Name: league.Name,
            Description: league.Description,
            OwnerId: league.OwnerId,
            OwnerUsername: league.Owner.Username,
            IsPublic: league.IsPublic,
            MaxMembers: league.MaxMembers,
            CurrentMemberCount: league.Members.Count,
            ScoreExactMatch: league.ScoreExactMatch,
            ScoreCorrectResult: league.ScoreCorrectResult,
            BettingDeadlineMinutes: league.BettingDeadlineMinutes,
            AllowedCompetitionIds: allowedCompetitionIds,
            InviteCode: league.InviteCode,
            InviteCodeExpiresAt: league.InviteCodeExpiresAt,
            CreatedAt: league.CreatedAt));
    }

    private async Task<string> GenerateUniqueInviteCodeAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        for (var i = 0; i < maxAttempts; i++)
        {
            var code = inviteCodeGenerator.Generate();
            var exists = await context.Leagues
                .AnyAsync(l => l.InviteCode == code, cancellationToken);

            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Failed to generate unique invite code after multiple attempts");
    }
}
