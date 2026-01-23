using System.Data;
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
    DbSet<League> Leagues { get; }
    DbSet<LeagueMember> LeagueMembers { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);
}
