# Phase 4: League System - Detailed Implementation Plan

## Overview
Users can create and join betting leagues with configurable settings. Leagues support custom scoring rules, betting deadlines, competition filters, and invite-based membership. Following Clean Architecture patterns established in previous phases.

**Scope:** Backend only. Frontend UI deferred to Phase 6.

---

## Domain Layer

### Enums

#### MemberRole (`src/ExtraTime.Domain/Enums/MemberRole.cs`)
```csharp
namespace ExtraTime.Domain.Enums;

public enum MemberRole
{
    Member = 0,
    Owner = 1
}
```

### Entities

#### League (`src/ExtraTime.Domain/Entities/League.cs`)
```csharp
public sealed class League : BaseAuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Ownership
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    // Visibility & Size
    public bool IsPublic { get; set; } = false;
    public int MaxMembers { get; set; } = 255;

    // Scoring Rules
    public int ScoreExactMatch { get; set; } = 3;
    public int ScoreCorrectResult { get; set; } = 1;

    // Betting Rules
    public int BettingDeadlineMinutes { get; set; } = 5;

    // Competition Filter (null = all competitions allowed)
    public string? AllowedCompetitionIds { get; set; }  // JSON array of Guid[]

    // Invite System
    public required string InviteCode { get; set; }
    public DateTime? InviteCodeExpiresAt { get; set; }

    // Navigation
    public ICollection<LeagueMember> Members { get; set; } = [];
}
```

#### LeagueMember (`src/ExtraTime.Domain/Entities/LeagueMember.cs`)
```csharp
public sealed class LeagueMember : BaseEntity
{
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public MemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
```

---

## API Endpoints

| Method | Path | Description | Auth | Authorization |
|--------|------|-------------|------|---------------|
| POST | `/api/leagues` | Create league | Yes | Any user |
| GET | `/api/leagues` | List user's leagues | Yes | Any user |
| GET | `/api/leagues/{id}` | Get league details | Yes | League member |
| PUT | `/api/leagues/{id}` | Update league settings | Yes | Owner only |
| DELETE | `/api/leagues/{id}` | Delete league | Yes | Owner only |
| POST | `/api/leagues/{id}/join` | Join via invite code | Yes | Non-member |
| DELETE | `/api/leagues/{id}/leave` | Leave league | Yes | Member (not owner) |
| DELETE | `/api/leagues/{id}/members/{userId}` | Kick member | Yes | Owner only |
| POST | `/api/leagues/{id}/invite-code/regenerate` | Regenerate invite code | Yes | Owner only |

---

## Application Layer Structure

```
Features/Leagues/
├── LeagueErrors.cs
├── DTOs/LeagueDtos.cs
├── Commands/
│   ├── CreateLeague/
│   │   ├── CreateLeagueCommand.cs
│   │   ├── CreateLeagueCommandHandler.cs
│   │   └── CreateLeagueCommandValidator.cs
│   ├── UpdateLeague/
│   │   ├── UpdateLeagueCommand.cs
│   │   ├── UpdateLeagueCommandHandler.cs
│   │   └── UpdateLeagueCommandValidator.cs
│   ├── DeleteLeague/
│   │   ├── DeleteLeagueCommand.cs
│   │   └── DeleteLeagueCommandHandler.cs
│   ├── JoinLeague/
│   │   ├── JoinLeagueCommand.cs
│   │   ├── JoinLeagueCommandHandler.cs
│   │   └── JoinLeagueCommandValidator.cs
│   ├── LeaveLeague/
│   │   ├── LeaveLeagueCommand.cs
│   │   └── LeaveLeagueCommandHandler.cs
│   ├── KickMember/
│   │   ├── KickMemberCommand.cs
│   │   └── KickMemberCommandHandler.cs
│   └── RegenerateInviteCode/
│       ├── RegenerateInviteCodeCommand.cs
│       └── RegenerateInviteCodeCommandHandler.cs
└── Queries/
    ├── GetLeague/
    │   ├── GetLeagueQuery.cs
    │   └── GetLeagueQueryHandler.cs
    └── GetUserLeagues/
        ├── GetUserLeaguesQuery.cs
        └── GetUserLeaguesQueryHandler.cs
```

---

## Interfaces to Add

### IInviteCodeGenerator (`src/ExtraTime.Application/Common/Interfaces/IInviteCodeGenerator.cs`)
```csharp
public interface IInviteCodeGenerator
{
    string Generate();
}
```

**Purpose:** Generate unique 8-character alphanumeric invite codes (e.g., "A3K9XM2P")

---

## Infrastructure Services

### InviteCodeGenerator (`src/ExtraTime.Infrastructure/Services/InviteCodeGenerator.cs`)
```csharp
public sealed class InviteCodeGenerator : IInviteCodeGenerator
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude ambiguous chars
    private const int CodeLength = 8;

    public string Generate()
    {
        // Generate cryptographically secure random code
    }
}
```

---

## Application DTOs

### Request DTOs

**CreateLeagueRequest:**
```csharp
public sealed record CreateLeagueRequest(
    string Name,
    string? Description,
    bool IsPublic,
    int MaxMembers,
    int ScoreExactMatch,
    int ScoreCorrectResult,
    int BettingDeadlineMinutes,
    Guid[]? AllowedCompetitionIds,
    DateTime? InviteCodeExpiresAt
);
```

**UpdateLeagueRequest:**
```csharp
public sealed record UpdateLeagueRequest(
    string Name,
    string? Description,
    bool IsPublic,
    int MaxMembers,
    int ScoreExactMatch,
    int ScoreCorrectResult,
    int BettingDeadlineMinutes,
    Guid[]? AllowedCompetitionIds
);
```

**JoinLeagueRequest:**
```csharp
public sealed record JoinLeagueRequest(string InviteCode);
```

**RegenerateInviteCodeRequest:**
```csharp
public sealed record RegenerateInviteCodeRequest(DateTime? ExpiresAt);
```

### Response DTOs

**LeagueDto:**
```csharp
public sealed record LeagueDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string OwnerUsername,
    bool IsPublic,
    int MaxMembers,
    int CurrentMemberCount,
    int ScoreExactMatch,
    int ScoreCorrectResult,
    int BettingDeadlineMinutes,
    Guid[]? AllowedCompetitionIds,
    string InviteCode,
    DateTime? InviteCodeExpiresAt,
    DateTime CreatedAt
);
```

**LeagueSummaryDto:** (for list view)
```csharp
public sealed record LeagueSummaryDto(
    Guid Id,
    string Name,
    string OwnerUsername,
    int MemberCount,
    bool IsPublic,
    DateTime CreatedAt
);
```

**LeagueMemberDto:**
```csharp
public sealed record LeagueMemberDto(
    Guid UserId,
    string Username,
    string Email,
    MemberRole Role,
    DateTime JoinedAt
);
```

**LeagueDetailDto:** (includes members)
```csharp
public sealed record LeagueDetailDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string OwnerUsername,
    bool IsPublic,
    int MaxMembers,
    int ScoreExactMatch,
    int ScoreCorrectResult,
    int BettingDeadlineMinutes,
    Guid[]? AllowedCompetitionIds,
    string InviteCode,
    DateTime? InviteCodeExpiresAt,
    DateTime CreatedAt,
    List<LeagueMemberDto> Members
);
```

---

## Validation Rules

### CreateLeague
- **Name:** Required, 3-100 chars
- **Description:** Optional, max 500 chars
- **MaxMembers:** 2-255
- **ScoreExactMatch:** 0-100
- **ScoreCorrectResult:** 0-100
- **BettingDeadlineMinutes:** 0-120 (0 = no deadline, up to 2 hours before match)
- **AllowedCompetitionIds:** If provided, must contain valid competition IDs
- **InviteCodeExpiresAt:** If provided, must be future date

### UpdateLeague
- Same as CreateLeague (except no InviteCodeExpiresAt)
- Only owner can update

### JoinLeague
- **InviteCode:** Required, 8 chars
- League must exist
- Code must not be expired
- League must not be full
- User must not already be a member

### LeaveLeague
- User must be a member
- User must not be the owner

### KickMember
- Target user must be a member
- Target user must not be the owner
- Requester must be the owner

---

## Business Rules

1. **Ownership:**
   - Creator automatically becomes owner
   - Owner is added as LeagueMember with Role = Owner
   - Owner cannot leave league (must delete instead)
   - Owner cannot be kicked

2. **Membership:**
   - Joining requires valid, non-expired invite code
   - League cannot exceed MaxMembers
   - Users can be in multiple leagues
   - Only non-owners can leave
   - Only owner can kick members

3. **Deletion:**
   - Only owner can delete league
   - Deleting league removes all members (cascade)
   - Bets in deleted league are preserved for history (Phase 5)

4. **Invite Codes:**
   - Auto-generated on creation (8-char alphanumeric)
   - Owner can regenerate at any time
   - Regeneration invalidates old code
   - Optional expiration date
   - Codes are case-insensitive when joining

5. **Settings:**
   - AllowedCompetitionIds = null means all competitions
   - BettingDeadlineMinutes enforced when placing bets (Phase 5)
   - Scoring rules used for bet calculation (Phase 5)

---

## Error Messages

### LeagueErrors.cs
```csharp
public static class LeagueErrors
{
    public const string LeagueNotFound = "League not found";
    public const string NotAMember = "You are not a member of this league";
    public const string NotTheOwner = "Only the league owner can perform this action";
    public const string InvalidInviteCode = "Invalid or expired invite code";
    public const string LeagueFull = "This league is full";
    public const string AlreadyAMember = "You are already a member of this league";
    public const string OwnerCannotLeave = "League owner cannot leave. Delete the league instead";
    public const string CannotKickOwner = "Cannot kick the league owner";
    public const string MemberNotFound = "Member not found in this league";
}
```

---

## Infrastructure EF Core Configurations

### LeagueConfiguration.cs
```csharp
public sealed class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.Property(l => l.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.Description)
            .HasMaxLength(500);

        builder.Property(l => l.InviteCode)
            .HasMaxLength(8)
            .IsRequired();

        builder.HasIndex(l => l.InviteCode)
            .IsUnique();

        builder.HasOne(l => l.Owner)
            .WithMany()
            .HasForeignKey(l => l.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### LeagueMemberConfiguration.cs
```csharp
public sealed class LeagueMemberConfiguration : IEntityTypeConfiguration<LeagueMember>
{
    public void Configure(EntityTypeBuilder<LeagueMember> builder)
    {
        builder.HasOne(lm => lm.League)
            .WithMany(l => l.Members)
            .HasForeignKey(lm => lm.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lm => lm.User)
            .WithMany()
            .HasForeignKey(lm => lm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: user can only be in a league once
        builder.HasIndex(lm => new { lm.LeagueId, lm.UserId })
            .IsUnique();
    }
}
```

---

## Implementation Order

### Step 1: Domain Layer
1. Create MemberRole enum
2. Create League entity
3. Create LeagueMember entity

### Step 2: Application Common
1. Create IInviteCodeGenerator interface
2. Update IApplicationDbContext with League and LeagueMember DbSets

### Step 3: Application Features - DTOs & Errors
1. Create LeagueDtos.cs (all request/response DTOs)
2. Create LeagueErrors.cs

### Step 4: Application Features - Commands
1. CreateLeague (command + handler + validator)
2. UpdateLeague (command + handler + validator)
3. DeleteLeague (command + handler)
4. JoinLeague (command + handler + validator)
5. LeaveLeague (command + handler)
6. KickMember (command + handler)
7. RegenerateInviteCode (command + handler)

### Step 5: Application Features - Queries
1. GetUserLeagues (query + handler) - returns LeagueSummaryDto[]
2. GetLeague (query + handler) - returns LeagueDetailDto

### Step 6: Infrastructure Services
1. Create InviteCodeGenerator service
2. Update DependencyInjection.cs to register service

### Step 7: Infrastructure EF Core
1. Create LeagueConfiguration
2. Create LeagueMemberConfiguration
3. Update ApplicationDbContext with new DbSets and configurations

### Step 8: API Layer
1. Create LeagueEndpoints.cs with all 9 endpoints
2. Update Program.cs to map endpoints

### Step 9: Database Migration
1. Create migration: `dotnet ef migrations add AddLeagueSystem`
2. Apply migration: `dotnet ef database update`

---

## Files to Create (New)

### Domain
- `src/ExtraTime.Domain/Enums/MemberRole.cs`
- `src/ExtraTime.Domain/Entities/League.cs`
- `src/ExtraTime.Domain/Entities/LeagueMember.cs`

### Application - Interfaces
- `src/ExtraTime.Application/Common/Interfaces/IInviteCodeGenerator.cs`

### Application - Features
- `src/ExtraTime.Application/Features/Leagues/LeagueErrors.cs`
- `src/ExtraTime.Application/Features/Leagues/DTOs/LeagueDtos.cs`
- `src/ExtraTime.Application/Features/Leagues/Commands/CreateLeague/*` (3 files)
- `src/ExtraTime.Application/Features/Leagues/Commands/UpdateLeague/*` (3 files)
- `src/ExtraTime.Application/Features/Leagues/Commands/DeleteLeague/*` (2 files)
- `src/ExtraTime.Application/Features/Leagues/Commands/JoinLeague/*` (3 files)
- `src/ExtraTime.Application/Features/Leagues/Commands/LeaveLeague/*` (2 files)
- `src/ExtraTime.Application/Features/Leagues/Commands/KickMember/*` (2 files)
- `src/ExtraTime.Application/Features/Leagues/Commands/RegenerateInviteCode/*` (2 files)
- `src/ExtraTime.Application/Features/Leagues/Queries/GetUserLeagues/*` (2 files)
- `src/ExtraTime.Application/Features/Leagues/Queries/GetLeague/*` (2 files)

### Infrastructure
- `src/ExtraTime.Infrastructure/Services/InviteCodeGenerator.cs`
- `src/ExtraTime.Infrastructure/Data/Configurations/LeagueConfiguration.cs`
- `src/ExtraTime.Infrastructure/Data/Configurations/LeagueMemberConfiguration.cs`

### API
- `src/ExtraTime.API/Features/Leagues/LeagueEndpoints.cs`

---

## Files to Modify (Existing)

- `src/ExtraTime.Application/Common/Interfaces/IApplicationDbContext.cs` - Add DbSets
- `src/ExtraTime.Infrastructure/Data/ApplicationDbContext.cs` - Add DbSets
- `src/ExtraTime.Infrastructure/DependencyInjection.cs` - Register InviteCodeGenerator
- `src/ExtraTime.API/Program.cs` - Map LeagueEndpoints

---

## Verification Steps

### Build & Migration
```bash
dotnet build
dotnet ef migrations add AddLeagueSystem --project src/ExtraTime.Infrastructure --startup-project src/ExtraTime.API
dotnet ef database update --project src/ExtraTime.Infrastructure --startup-project src/ExtraTime.API
```

### API Testing (Swagger)

1. **Authentication:**
   - Login as user1 and user2 to get JWT tokens

2. **Create League (user1):**
   ```json
   POST /api/leagues
   {
     "name": "Friends League",
     "description": "Our friendly competition",
     "isPublic": false,
     "maxMembers": 10,
     "scoreExactMatch": 3,
     "scoreCorrectResult": 1,
     "bettingDeadlineMinutes": 5,
     "allowedCompetitionIds": null,
     "inviteCodeExpiresAt": null
   }
   ```
   - Verify 201 Created with LeagueDto
   - Save invite code

3. **Get User Leagues (user1):**
   ```
   GET /api/leagues
   ```
   - Verify returns array with created league
   - Verify user1 is owner

4. **Get League Details (user1):**
   ```
   GET /api/leagues/{id}
   ```
   - Verify returns LeagueDetailDto
   - Verify Members array contains user1 with Role = Owner

5. **Join League (user2):**
   ```json
   POST /api/leagues/{id}/join
   {
     "inviteCode": "A3K9XM2P"
   }
   ```
   - Verify 200 OK
   - Verify user2 now in Members with Role = Member

6. **Update League (user1):**
   ```json
   PUT /api/leagues/{id}
   {
     "name": "Updated League Name",
     ...other fields
   }
   ```
   - Verify 200 OK

7. **Update League (user2):**
   - Verify 403 Forbidden (not owner)

8. **Leave League (user2):**
   ```
   DELETE /api/leagues/{id}/leave
   ```
   - Verify 204 No Content
   - Verify user2 no longer in Members

9. **Leave League (user1 - owner):**
   - Verify 400 Bad Request with "OwnerCannotLeave" error

10. **Rejoin (user2) & Kick Member (user1):**
    ```
    DELETE /api/leagues/{id}/members/{user2Id}
    ```
    - Verify 204 No Content
    - Verify user2 removed from Members

11. **Regenerate Invite Code (user1):**
    ```json
    POST /api/leagues/{id}/invite-code/regenerate
    {
      "expiresAt": "2026-12-31T23:59:59Z"
    }
    ```
    - Verify 200 OK with new code
    - Try joining with old code - verify 400 Bad Request

12. **Join with Expired Code:**
    - Create league with past expiry date
    - Try to join - verify 400 Bad Request

13. **Join Full League:**
    - Create league with maxMembers = 2
    - Join with user2
    - Try to join with user3 - verify 400 Bad Request

14. **Delete League (user1):**
    ```
    DELETE /api/leagues/{id}
    ```
    - Verify 204 No Content
    - Verify league no longer exists
    - Verify all members removed (cascade)

15. **Validation Errors:**
    - Try creating league with name = "" → 400 Bad Request
    - Try creating league with maxMembers = 300 → 400 Bad Request
    - Try creating league with invalid competition IDs → 400 Bad Request

---

## Edge Cases to Handle

1. **Concurrent Joins:** Two users join simultaneously when 1 spot left
   - Use transaction + check member count before insert
2. **Code Collisions:** Generated invite code already exists
   - Retry generation with max attempts
3. **Case Sensitivity:** User enters "a3k9xm2p" instead of "A3K9XM2P"
   - Normalize to uppercase before comparison
4. **Owner Deletion:** Owner account deleted
   - Use DeleteBehavior.Restrict on Owner FK
5. **Kicked Member Rejoins:** Member kicked but has old invite code
   - No restriction - kicked members can rejoin if they have valid code

---

## Security Considerations

1. **Authorization Checks:**
   - GetLeague: Verify user is member
   - UpdateLeague: Verify user is owner
   - DeleteLeague: Verify user is owner
   - KickMember: Verify requester is owner, target is not owner
   - LeaveLeague: Verify user is member and not owner

2. **Input Validation:**
   - Sanitize league name and description
   - Validate competition IDs exist in database
   - Prevent XSS in user-generated content

3. **Rate Limiting:**
   - Consider rate limiting invite code regeneration
   - Prevent brute-force invite code guessing (8 chars = 36^8 combinations)

---

## Future Enhancements (Not in Phase 4)

- League search/discovery for public leagues
- League privacy settings (public vs private vs invite-only)
- Member activity tracking (last bet, total bets)
- League chat/discussion
- League achievements/trophies
- Sub-leagues or divisions
- Waiting list for full leagues

---

## Notes

- Frontend implementation deferred to Phase 6
- Bet placement and scoring functionality deferred to Phase 5
- AllowedCompetitionIds stored as JSON string for simplicity (could be normalized to junction table in future)
- InviteCode collisions handled with retry logic (probability is extremely low with 36^8 combinations)
- Leagues are soft-deletable via BaseAuditableEntity (DeletedAt, DeletedBy)
