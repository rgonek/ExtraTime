using Bogus;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.UnitTests.TestData;

public sealed class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _email = new Faker().Internet.Email();
    private string _username = new Faker().Internet.UserName();
    private string _passwordHash = "$2a$11$test.hash.value";
    private UserRole _role = UserRole.User;

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public UserBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    public User Build()
    {
        var user = User.Register(_email, _username, _passwordHash, _role);
        user.Id = _id;
        user.CreatedAt = Clock.UtcNow;
        user.UpdatedAt = Clock.UtcNow;
        return user;
    }
}

public sealed class LeagueBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = new Faker().Company.CompanyName();
    private string? _description = null;
    private Guid _ownerId = Guid.NewGuid();
    private bool _isPublic = false;
    private int _maxMembers = 255;
    private int _scoreExactMatch = 3;
    private int _scoreCorrectResult = 1;
    private int _bettingDeadlineMinutes = 5;
    private string _inviteCode = new Faker().Random.AlphaNumeric(8).ToUpper();

    public LeagueBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public LeagueBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public LeagueBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public LeagueBuilder WithOwnerId(Guid ownerId)
    {
        _ownerId = ownerId;
        return this;
    }

    public LeagueBuilder WithPublic(bool isPublic)
    {
        _isPublic = isPublic;
        return this;
    }

    public LeagueBuilder WithMaxMembers(int maxMembers)
    {
        _maxMembers = maxMembers;
        return this;
    }

    public LeagueBuilder WithScoringRules(int exactMatch, int correctResult)
    {
        _scoreExactMatch = exactMatch;
        _scoreCorrectResult = correctResult;
        return this;
    }

    public LeagueBuilder WithInviteCode(string inviteCode)
    {
        _inviteCode = inviteCode;
        return this;
    }

    public League Build()
    {
        var league = League.Create(
            _name,
            _ownerId,
            _inviteCode,
            _description,
            _isPublic,
            _maxMembers,
            _scoreExactMatch,
            _scoreCorrectResult,
            _bettingDeadlineMinutes);

        league.Id = _id;
        league.CreatedAt = Clock.UtcNow;
        league.UpdatedAt = Clock.UtcNow;
        return league;
    }
}

public sealed class CompetitionBuilder
{
    private Guid _id = Guid.NewGuid();
    private int _externalId = new Faker().Random.Int(1000, 9999);
    private string _name = new Faker().Company.CompanyName() + " League";
    private string _code = new Faker().Random.AlphaNumeric(3).ToUpper();
    private string _country = new Faker().Address.Country();

    public CompetitionBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public CompetitionBuilder WithExternalId(int externalId)
    {
        _externalId = externalId;
        return this;
    }

    public CompetitionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CompetitionBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public Competition Build()
    {
        return new Competition
        {
            Id = _id,
            ExternalId = _externalId,
            Name = _name,
            Code = _code,
            Country = _country,
            LastSyncedAt = Clock.UtcNow
        };
    }
}

public sealed class TeamBuilder
{
    private Guid _id = Guid.NewGuid();
    private int _externalId = new Faker().Random.Int(1, 9999);
    private string _name = new Faker().Company.CompanyName() + " FC";
    private string _shortName = new Faker().Random.AlphaNumeric(3).ToUpper();

    public TeamBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public TeamBuilder WithExternalId(int externalId)
    {
        _externalId = externalId;
        return this;
    }

    public TeamBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TeamBuilder WithShortName(string shortName)
    {
        _shortName = shortName;
        return this;
    }

    public Team Build()
    {
        return new Team
        {
            Id = _id,
            ExternalId = _externalId,
            Name = _name,
            ShortName = _shortName,
            LastSyncedAt = Clock.UtcNow
        };
    }
}

public sealed class MatchBuilder
{
    private Guid _id = Guid.NewGuid();
    private int _externalId = new Faker().Random.Int(10000, 99999);
    private Guid _competitionId = Guid.NewGuid();
    private Guid _homeTeamId = Guid.NewGuid();
    private Guid _awayTeamId = Guid.NewGuid();
    private DateTime _matchDateUtc = Clock.UtcNow.AddDays(1);
    private MatchStatus _status = MatchStatus.Scheduled;
    private int? _homeScore = null;
    private int? _awayScore = null;

    public MatchBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public MatchBuilder WithExternalId(int externalId)
    {
        _externalId = externalId;
        return this;
    }

    public MatchBuilder WithCompetitionId(Guid competitionId)
    {
        _competitionId = competitionId;
        return this;
    }

    public MatchBuilder WithTeams(Guid homeTeamId, Guid awayTeamId)
    {
        _homeTeamId = homeTeamId;
        _awayTeamId = awayTeamId;
        return this;
    }

    public MatchBuilder WithMatchDate(DateTime matchDateUtc)
    {
        _matchDateUtc = matchDateUtc;
        return this;
    }

    public MatchBuilder WithStatus(MatchStatus status)
    {
        _status = status;
        return this;
    }

    public MatchBuilder WithScore(int homeScore, int awayScore)
    {
        _homeScore = homeScore;
        _awayScore = awayScore;
        return this;
    }

    public Match Build()
    {
        var match = Match.Create(
            _externalId,
            _competitionId,
            _homeTeamId,
            _awayTeamId,
            _matchDateUtc,
            _status);

        match.Id = _id;
        if (_homeScore.HasValue && _awayScore.HasValue)
        {
            match.UpdateScore(_homeScore.Value, _awayScore.Value);
        }

        return match;
    }
}

public sealed class BetBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _leagueId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private Guid _matchId = Guid.NewGuid();
    private int _predictedHomeScore = 2;
    private int _predictedAwayScore = 1;
    private DateTime _placedAt = Clock.UtcNow;

    public BetBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public BetBuilder WithLeagueId(Guid leagueId)
    {
        _leagueId = leagueId;
        return this;
    }

    public BetBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public BetBuilder WithMatchId(Guid matchId)
    {
        _matchId = matchId;
        return this;
    }

    public BetBuilder WithPrediction(int homeScore, int awayScore)
    {
        _predictedHomeScore = homeScore;
        _predictedAwayScore = awayScore;
        return this;
    }

    public BetBuilder WithPlacedAt(DateTime placedAt)
    {
        _placedAt = placedAt;
        return this;
    }

    public Bet Build()
    {
        var bet = Bet.Place(
            _leagueId,
            _userId,
            _matchId,
            _predictedHomeScore,
            _predictedAwayScore);

        bet.Id = _id;
        bet.CreatedAt = Clock.UtcNow;
        bet.UpdatedAt = Clock.UtcNow;
        return bet;
    }
}
