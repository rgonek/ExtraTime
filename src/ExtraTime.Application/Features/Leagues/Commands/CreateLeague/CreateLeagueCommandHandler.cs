using System.Text.Json;
using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Leagues.DTOs;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Leagues.Commands.CreateLeague;

public sealed class CreateLeagueCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IInviteCodeGenerator inviteCodeGenerator) : IRequestHandler<CreateLeagueCommand, Result<LeagueDto>>
{
    public async ValueTask<Result<LeagueDto>> Handle(
        CreateLeagueCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        // Validate competition IDs if provided
        if (request.AllowedCompetitionIds is { Length: > 0 })
        {
            var validCompetitionCount = await context.Competitions
                .Where(c => request.AllowedCompetitionIds.Contains(c.Id))
                .CountAsync(cancellationToken);

            if (validCompetitionCount != request.AllowedCompetitionIds.Length)
            {
                return Result<LeagueDto>.Failure("One or more competition IDs are invalid");
            }
        }

        // Generate unique invite code
        var inviteCode = await GenerateUniqueInviteCodeAsync(cancellationToken);

        var league = new League
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = userId,
            IsPublic = request.IsPublic,
            MaxMembers = request.MaxMembers,
            ScoreExactMatch = request.ScoreExactMatch,
            ScoreCorrectResult = request.ScoreCorrectResult,
            BettingDeadlineMinutes = request.BettingDeadlineMinutes,
            AllowedCompetitionIds = request.AllowedCompetitionIds != null
                ? JsonSerializer.Serialize(request.AllowedCompetitionIds)
                : null,
            InviteCode = inviteCode,
            InviteCodeExpiresAt = request.InviteCodeExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        // Add owner as member
        var ownerMember = new LeagueMember
        {
            League = league,
            UserId = userId,
            Role = MemberRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        context.Leagues.Add(league);
        context.LeagueMembers.Add(ownerMember);
        await context.SaveChangesAsync(cancellationToken);

        // Get owner username for response
        var owner = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Username })
            .FirstAsync(cancellationToken);

        return Result<LeagueDto>.Success(new LeagueDto(
            Id: league.Id,
            Name: league.Name,
            Description: league.Description,
            OwnerId: league.OwnerId,
            OwnerUsername: owner.Username,
            IsPublic: league.IsPublic,
            MaxMembers: league.MaxMembers,
            CurrentMemberCount: 1,
            ScoreExactMatch: league.ScoreExactMatch,
            ScoreCorrectResult: league.ScoreCorrectResult,
            BettingDeadlineMinutes: league.BettingDeadlineMinutes,
            AllowedCompetitionIds: request.AllowedCompetitionIds,
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
