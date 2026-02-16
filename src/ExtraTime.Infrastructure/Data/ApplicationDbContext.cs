using System.Data;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Infrastructure.Data;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUserService? currentUserService = null,
    IMediator? mediator = null)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<CompetitionTeam> CompetitionTeams => Set<CompetitionTeam>();
    public DbSet<SeasonTeam> SeasonTeams => Set<SeasonTeam>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<LeagueMember> LeagueMembers => Set<LeagueMember>();
    public DbSet<LeagueBotMember> LeagueBotMembers => Set<LeagueBotMember>();
    public DbSet<Bet> Bets => Set<Bet>();
    public DbSet<BetResult> BetResults => Set<BetResult>();
    public DbSet<LeagueStanding> LeagueStandings => Set<LeagueStanding>();
    public DbSet<Bot> Bots => Set<Bot>();
    public DbSet<FootballStanding> FootballStandings => Set<FootballStanding>();
    public DbSet<TeamFormCache> TeamFormCaches => Set<TeamFormCache>();
    public DbSet<HeadToHead> HeadToHeads => Set<HeadToHead>();
    public DbSet<IntegrationStatus> IntegrationStatuses => Set<IntegrationStatus>();
    public DbSet<TeamXgStats> TeamXgStats => Set<TeamXgStats>();
    public DbSet<MatchXgStats> MatchXgStats => Set<MatchXgStats>();
    public DbSet<MatchOdds> MatchOdds => Set<MatchOdds>();
    public DbSet<MatchStats> MatchStats => Set<MatchStats>();
    public DbSet<TeamEloRating> TeamEloRatings => Set<TeamEloRating>();
    public DbSet<TeamInjuries> TeamInjuries => Set<TeamInjuries>();
    public DbSet<TeamXgSnapshot> TeamXgSnapshots => Set<TeamXgSnapshot>();
    public DbSet<TeamInjurySnapshot> TeamInjurySnapshots => Set<TeamInjurySnapshot>();
    public DbSet<PlayerInjury> PlayerInjuries => Set<PlayerInjury>();
    public DbSet<TeamSuspensions> TeamSuspensions => Set<TeamSuspensions>();
    public DbSet<PlayerSuspension> PlayerSuspensions => Set<PlayerSuspension>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = Clock.UtcNow;
        var userId = currentUserService?.UserId?.ToString();

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync()
    {
        if (mediator is null)
            return;

        var entities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        // Check if using InMemory database - transactions are not supported
        if (Database.ProviderName?.Contains("InMemory") == true)
        {
            // Execute without transaction for InMemory database
            return await operation(cancellationToken);
        }

        var strategy = Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async ct =>
        {
            // If already in a transaction, just execute the operation
            if (Database.CurrentTransaction != null)
            {
                return await operation(ct);
            }

            await using var transaction = await Database.BeginTransactionAsync(isolationLevel, ct);
            try
            {
                var result = await operation(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }, cancellationToken);
    }
}
