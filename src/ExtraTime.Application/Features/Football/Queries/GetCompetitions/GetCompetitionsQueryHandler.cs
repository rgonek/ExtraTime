using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Football.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Football.Queries.GetCompetitions;

public sealed class GetCompetitionsQueryHandler(
    IApplicationDbContext context) : IRequestHandler<GetCompetitionsQuery, Result<IReadOnlyList<CompetitionDto>>>
{
    public async ValueTask<Result<IReadOnlyList<CompetitionDto>>> Handle(
        GetCompetitionsQuery request,
        CancellationToken cancellationToken)
    {
        var competitions = await context.Competitions
            .OrderBy(c => c.Name)
            .Select(c => new CompetitionDto(
                c.Id,
                c.ExternalId,
                c.Name,
                c.Code,
                c.Type,
                c.Country,
                c.LogoUrl,
                c.CurrentMatchday,
                c.CurrentSeasonStart,
                c.CurrentSeasonEnd,
                c.LastSyncedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CompetitionDto>>.Success(competitions);
    }
}
