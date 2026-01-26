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

        // Update league settings using domain logic
        try
        {
            league.UpdateSettings(
                request.Name,
                request.Description,
                request.IsPublic,
                request.MaxMembers,
                request.ScoreExactMatch,
                request.ScoreCorrectResult,
                request.BettingDeadlineMinutes);
            
            league.SetCompetitionFilter(allowedCompetitionIds);
        }
        catch (Exception ex)
        {
            return Result<LeagueDto>.Failure(ex.Message);
        }

        await context.SaveChangesAsync(cancellationToken);

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
}
