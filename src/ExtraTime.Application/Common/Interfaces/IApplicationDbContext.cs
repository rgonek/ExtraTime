using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<BackgroundJob> BackgroundJobs { get; }
    DbSet<Competition> Competitions { get; }
    DbSet<Team> Teams { get; }
    DbSet<CompetitionTeam> CompetitionTeams { get; }
    DbSet<Match> Matches { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
