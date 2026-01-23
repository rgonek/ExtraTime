using System.Text.Json;
using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Leagues.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Leagues.Commands.UpdateLeague;

public sealed class UpdateLeagueCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateLeagueCommand, Result<LeagueDto>>
{
    public async ValueTask<Result<LeagueDto>> Handle(
        UpdateLeagueCommand request,
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

        // Check if new MaxMembers is less than current member count
        var currentMemberCount = league.Members.Count;
        if (request.MaxMembers < currentMemberCount)
        {
            return Result<LeagueDto>.Failure($"Cannot set max members to {request.MaxMembers} when league already has {currentMemberCount} members");
        }

        // Update league properties
        league.Name = request.Name;
        league.Description = request.Description;
        league.IsPublic = request.IsPublic;
        league.MaxMembers = request.MaxMembers;
        league.ScoreExactMatch = request.ScoreExactMatch;
        league.ScoreCorrectResult = request.ScoreCorrectResult;
        league.BettingDeadlineMinutes = request.BettingDeadlineMinutes;
        league.AllowedCompetitionIds = request.AllowedCompetitionIds != null
            ? JsonSerializer.Serialize(request.AllowedCompetitionIds)
            : null;
        league.UpdatedAt = DateTime.UtcNow;
        league.UpdatedBy = userId;

        await context.SaveChangesAsync(cancellationToken);

        return Result<LeagueDto>.Success(new LeagueDto(
            Id: league.Id,
            Name: league.Name,
            Description: league.Description,
            OwnerId: league.OwnerId,
            OwnerUsername: league.Owner.Username,
            IsPublic: league.IsPublic,
            MaxMembers: league.MaxMembers,
            CurrentMemberCount: currentMemberCount,
            ScoreExactMatch: league.ScoreExactMatch,
            ScoreCorrectResult: league.ScoreCorrectResult,
            BettingDeadlineMinutes: league.BettingDeadlineMinutes,
            AllowedCompetitionIds: request.AllowedCompetitionIds,
            InviteCode: league.InviteCode,
            InviteCodeExpiresAt: league.InviteCodeExpiresAt,
            CreatedAt: league.CreatedAt));
    }
}
