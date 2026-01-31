using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.Events;

namespace ExtraTime.Domain.Entities;

public sealed class League : BaseAuditableEntity
{
    private readonly List<LeagueMember> _members = [];

    // Properties with private setters for encapsulation
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    // Ownership
    public Guid OwnerId { get; private set; }
    public User Owner { get; private set; } = null!;

    // Visibility & Size
    public bool IsPublic { get; private set; }
    public int MaxMembers { get; private set; }

    // Scoring Rules
    public int ScoreExactMatch { get; private set; }
    public int ScoreCorrectResult { get; private set; }

    // Betting Rules
    public int BettingDeadlineMinutes { get; private set; }

    // Competition Filter (null = all competitions allowed)
    public string? AllowedCompetitionIds { get; private set; }  // JSON array of Guid[]

    // Invite System
    public string InviteCode { get; private set; } = null!;
    public DateTime? InviteCodeExpiresAt { get; private set; }

    // Bot System
    public bool BotsEnabled { get; private set; }

    // Navigation
    public IReadOnlyCollection<LeagueMember> Members => _members.AsReadOnly();

    private readonly List<LeagueBotMember> _botMembers = [];
    public IReadOnlyCollection<LeagueBotMember> BotMembers => _botMembers.AsReadOnly();

    private League() { } // Required for EF Core

    public static League Create(
        string name,
        Guid ownerId,
        string inviteCode,
        string? description = null,
        bool isPublic = false,
        int maxMembers = 255,
        int scoreExactMatch = 3,
        int scoreCorrectResult = 1,
        int bettingDeadlineMinutes = 5)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("League name is required", nameof(name));

        if (maxMembers < 2)
            throw new ArgumentException("Max members must be at least 2", nameof(maxMembers));

        var league = new League
        {
            Name = name,
            OwnerId = ownerId,
            InviteCode = inviteCode,
            Description = description,
            IsPublic = isPublic,
            MaxMembers = maxMembers,
            ScoreExactMatch = scoreExactMatch,
            ScoreCorrectResult = scoreCorrectResult,
            BettingDeadlineMinutes = bettingDeadlineMinutes
        };

        // Owner is automatically a member
        league.AddMember(ownerId, MemberRole.Owner);
        
        league.AddDomainEvent(new LeagueCreated(league.Id, ownerId));
        
        return league;
    }

    public void AddMember(Guid userId, MemberRole role)
    {
        if (_members.Count >= MaxMembers)
            throw new InvalidOperationException("League is full");

        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this league");

        var member = LeagueMember.Create(Id, userId, role);

        _members.Add(member);
        AddDomainEvent(new LeagueMemberAdded(Id, userId));
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return;

        if (member.Role == MemberRole.Owner)
            throw new InvalidOperationException("Owner cannot be removed from the league. Transfer ownership first or delete the league.");

        _members.Remove(member);
        AddDomainEvent(new LeagueMemberRemoved(Id, userId));
    }

    public void RegenerateInviteCode(string newCode, DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(newCode))
            throw new ArgumentException("Invite code is required", nameof(newCode));

        InviteCode = newCode;
        InviteCodeExpiresAt = expiresAt;
        
        AddDomainEvent(new LeagueInviteCodeRegenerated(Id, newCode));
    }

    public void UpdateSettings(
        string name,
        string? description,
        bool isPublic,
        int maxMembers,
        int scoreExactMatch,
        int scoreCorrectResult,
        int bettingDeadlineMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("League name is required", nameof(name));

        if (maxMembers < _members.Count)
            throw new InvalidOperationException("Max members cannot be less than the current number of members");

        Name = name;
        Description = description;
        IsPublic = isPublic;
        MaxMembers = maxMembers;
        ScoreExactMatch = scoreExactMatch;
        ScoreCorrectResult = scoreCorrectResult;
        BettingDeadlineMinutes = bettingDeadlineMinutes;
    }

    public bool CanAcceptBet(Guid competitionId)
    {
        if (string.IsNullOrEmpty(AllowedCompetitionIds)) return true;

        try
        {
            var allowedIds = System.Text.Json.JsonSerializer.Deserialize<Guid[]>(AllowedCompetitionIds);
            return allowedIds == null || allowedIds.Length == 0 || allowedIds.Contains(competitionId);
        }
        catch
        {
            return true; // Fallback to allowed if JSON is corrupted
        }
    }

    public void SetCompetitionFilter(IEnumerable<Guid>? competitionIds)
    {
        if (competitionIds == null || !competitionIds.Any())
        {
            AllowedCompetitionIds = null;
        }
        else
        {
            AllowedCompetitionIds = System.Text.Json.JsonSerializer.Serialize(competitionIds.ToArray());
        }
    }

    public void EnableBots(bool enabled)
    {
        BotsEnabled = enabled;
    }

    public void AddBot(Guid botId)
    {
        if (_botMembers.Any(bm => bm.BotId == botId))
            throw new InvalidOperationException("Bot is already a member of this league");

        var botMember = new LeagueBotMember
        {
            LeagueId = Id,
            BotId = botId,
            AddedAt = Clock.UtcNow
        };

        _botMembers.Add(botMember);
    }

    public void RemoveBot(Guid botId)
    {
        var botMember = _botMembers.FirstOrDefault(bm => bm.BotId == botId);
        if (botMember != null)
        {
            _botMembers.Remove(botMember);
        }
    }
}
