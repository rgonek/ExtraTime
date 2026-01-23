using System.Text.Json;
using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Leagues.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Leagues.Queries.GetLeague;

public sealed class GetLeagueQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetLeagueQuery, Result<LeagueDetailDto>>
{
    public async ValueTask<Result<LeagueDetailDto>> Handle(
        GetLeagueQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var league = await context.Leagues
            .Include(l => l.Owner)
            .Include(l => l.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(l => l.Id == request.LeagueId, cancellationToken);

        if (league == null)
        {
            return Result<LeagueDetailDto>.Failure(LeagueErrors.LeagueNotFound);
        }

        // Check if user is a member
        var isMember = league.Members.Any(m => m.UserId == userId);
        if (!isMember)
        {
            return Result<LeagueDetailDto>.Failure(LeagueErrors.NotAMember);
        }

        // Parse AllowedCompetitionIds
        Guid[]? allowedCompetitionIds = null;
        if (!string.IsNullOrEmpty(league.AllowedCompetitionIds))
        {
            allowedCompetitionIds = JsonSerializer.Deserialize<Guid[]>(league.AllowedCompetitionIds);
        }

        var members = league.Members
            .OrderByDescending(m => m.Role)  // Owner first
            .ThenBy(m => m.JoinedAt)
            .Select(m => new LeagueMemberDto(
                UserId: m.UserId,
                Username: m.User.Username,
                Email: m.User.Email,
                Role: m.Role,
                JoinedAt: m.JoinedAt))
            .ToList();

        return Result<LeagueDetailDto>.Success(new LeagueDetailDto(
            Id: league.Id,
            Name: league.Name,
            Description: league.Description,
            OwnerId: league.OwnerId,
            OwnerUsername: league.Owner.Username,
            IsPublic: league.IsPublic,
            MaxMembers: league.MaxMembers,
            ScoreExactMatch: league.ScoreExactMatch,
            ScoreCorrectResult: league.ScoreCorrectResult,
            BettingDeadlineMinutes: league.BettingDeadlineMinutes,
            AllowedCompetitionIds: allowedCompetitionIds,
            InviteCode: league.InviteCode,
            InviteCodeExpiresAt: league.InviteCodeExpiresAt,
            CreatedAt: league.CreatedAt,
            Members: members));
    }
}
