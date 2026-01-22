using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<BackgroundJob> BackgroundJobs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
