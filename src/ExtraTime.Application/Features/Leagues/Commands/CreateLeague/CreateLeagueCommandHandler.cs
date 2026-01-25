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

        // Deduplicate and validate competition IDs if provided
        Guid[]? allowedCompetitionIds = null;
        if (request.AllowedCompetitionIds is { Length: > 0 })
        {
            allowedCompetitionIds = request.AllowedCompetitionIds.Distinct().ToArray();

            var validCompetitionCount = await context.Competitions
                .Where(c => allowedCompetitionIds.Contains(c.Id))
                .CountAsync(cancellationToken);

            if (validCompetitionCount != allowedCompetitionIds.Length)
            {
                return Result<LeagueDto>.Failure("One or more competition IDs are invalid");
            }
        }

        // Generate unique invite code
        var inviteCode = await inviteCodeGenerator.GenerateUniqueAsync(
            async (code, ct) => await context.Leagues.AnyAsync(l => l.InviteCode == code, ct),
            cancellationToken);

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
            AllowedCompetitionIds = allowedCompetitionIds != null
                ? JsonSerializer.Serialize(allowedCompetitionIds)
                : null,
            InviteCode = inviteCode,
            InviteCodeExpiresAt = request.InviteCodeExpiresAt
        };

        // Add owner as member
        var ownerMember = new LeagueMember
        {
            LeagueId = league.Id,
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
            AllowedCompetitionIds: allowedCompetitionIds,
            InviteCode: league.InviteCode,
            InviteCodeExpiresAt: league.InviteCodeExpiresAt,
            CreatedAt: league.CreatedAt));
    }
}
