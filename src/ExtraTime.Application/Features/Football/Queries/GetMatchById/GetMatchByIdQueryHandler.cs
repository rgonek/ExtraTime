using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Football.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Football.Queries.GetMatchById;

public sealed class GetMatchByIdQueryHandler(
    IApplicationDbContext context) : IRequestHandler<GetMatchByIdQuery, Result<MatchDetailDto>>
{
    public async ValueTask<Result<MatchDetailDto>> Handle(
        GetMatchByIdQuery request,
        CancellationToken cancellationToken)
    {
        var match = await context.Matches
            .Include(m => m.Competition)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => m.Id == request.MatchId)
            .Select(m => new MatchDetailDto(
                m.Id,
                new CompetitionSummaryDto(
                    m.Competition.Id,
                    m.Competition.Name,
                    m.Competition.Code,
                    m.Competition.Type,
                    m.Competition.Country,
                    m.Competition.LogoUrl),
                new TeamSummaryDto(
                    m.HomeTeam.Id,
                    m.HomeTeam.Name,
                    m.HomeTeam.ShortName,
                    m.HomeTeam.Tla,
                    m.HomeTeam.LogoUrl),
                new TeamSummaryDto(
                    m.AwayTeam.Id,
                    m.AwayTeam.Name,
                    m.AwayTeam.ShortName,
                    m.AwayTeam.Tla,
                    m.AwayTeam.LogoUrl),
                m.MatchDateUtc,
                m.Status,
                m.Matchday,
                m.Stage,
                m.Group,
                m.HomeScore,
                m.AwayScore,
                m.HomeHalfTimeScore,
                m.AwayHalfTimeScore,
                m.Venue,
                m.LastSyncedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (match is null)
        {
            return Result<MatchDetailDto>.Failure(FootballErrors.MatchNotFound);
        }

        return Result<MatchDetailDto>.Success(match);
    }
}
