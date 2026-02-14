using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Football.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Football.Queries.GetMatches;

public sealed class GetMatchesQueryHandler(
    IApplicationDbContext context) : IRequestHandler<GetMatchesQuery, Result<MatchesPagedResponse>>
{
    public async ValueTask<Result<MatchesPagedResponse>> Handle(
        GetMatchesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Matches
            .Include(m => m.Competition)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .AsQueryable();

        if (request.CompetitionId.HasValue)
        {
            query = query.Where(m => m.CompetitionId == request.CompetitionId.Value);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(m => m.MatchDateUtc >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(m => m.MatchDateUtc <= request.DateTo.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(m => m.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(m => m.MatchDateUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MatchDto(
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
                m.HomeScore,
                m.AwayScore))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return Result<MatchesPagedResponse>.Success(new MatchesPagedResponse(
            items,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages));
    }
}
