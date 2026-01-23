using ExtraTime.Application.Common.Interfaces;
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
            if (typeof(ExtraTime.Domain.Common.BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ExtraTime.Domain.Common.BaseAuditableEntity.DeletedAt));
                var nullConstant = System.Linq.Expressions.Expression.Constant(null, typeof(DateTime?));
                var filter = System.Linq.Expressions.Expression.Equal(property, nullConstant);
                var lambda = System.Linq.Expressions.Expression.Lambda(filter, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
