using System.Linq.Expressions;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<CompetitionTeam> CompetitionTeams => Set<CompetitionTeam>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<LeagueMember> LeagueMembers => Set<LeagueMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        // Apply global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseAuditableEntity.DeletedAt));
                var nullConstant = Expression.Constant(null, typeof(DateTime?));
                var filter = Expression.Equal(property, nullConstant);
                var lambda = Expression.Lambda(filter, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
