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
    DbSet<Season> Seasons { get; }
    DbSet<Team> Teams { get; }
    DbSet<CompetitionTeam> CompetitionTeams { get; }
    DbSet<SeasonTeam> SeasonTeams { get; }
    DbSet<Match> Matches { get; }
    DbSet<League> Leagues { get; }
    DbSet<LeagueMember> LeagueMembers { get; }
    DbSet<LeagueBotMember> LeagueBotMembers { get; }
    DbSet<Bet> Bets { get; }
    DbSet<Bot> Bots { get; }
    DbSet<BetResult> BetResults { get; }
    DbSet<LeagueStanding> LeagueStandings { get; }
    DbSet<FootballStanding> FootballStandings { get; }
    DbSet<TeamFormCache> TeamFormCaches { get; }
    DbSet<HeadToHead> HeadToHeads { get; }
    DbSet<IntegrationStatus> IntegrationStatuses { get; }
    DbSet<TeamXgStats> TeamXgStats { get; }
    DbSet<MatchXgStats> MatchXgStats { get; }
    DbSet<MatchOdds> MatchOdds { get; }
    DbSet<MatchStats> MatchStats { get; }
    DbSet<TeamEloRating> TeamEloRatings { get; }
    DbSet<TeamInjuries> TeamInjuries { get; }
    DbSet<PlayerInjury> PlayerInjuries { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);
}
