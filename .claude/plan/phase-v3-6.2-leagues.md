# Phase 6.2: League System UI

> **Goal:** Build complete league CRUD functionality with polished UI
> **Backend Analogy:** Implementing your first full feature with forms, lists, and details
> **Estimated Time:** 6-8 hours
> **Prerequisites:** Phase 6.1 complete (types and hooks ready)

---

## What You'll Learn

| Frontend Concept | Backend Analogy | Example |
|------------------|-----------------|---------|
| Component props | Method parameters | `LeagueCard({ league })` |
| State (useState) | Private fields | `const [name, setName] = useState('')` |
| Event handlers | Delegates/events | `onClick={handleSubmit}` |
| Conditional rendering | if/else in views | `{isOwner && <DeleteButton />}` |
| List rendering (map) | foreach/Select() | `leagues.map(l => <Card />)` |
| Controlled inputs | Two-way binding | `value={name} onChange={...}` |
| Form submission | Command handling | `onSubmit={handleCreate}` |

---

## Understanding React Patterns (For C# Developers)

### Props = Method Parameters (Immutable)

```typescript
// C# - Method with parameters
public LeagueCard RenderLeague(LeagueSummaryDto league, bool showOwner = true)
{
    // league is readonly, cannot modify
}

// React - Component with props
interface LeagueCardProps {
  league: LeagueSummaryDto;
  showOwner?: boolean;  // Optional with ? like C# optional param
}

function LeagueCard({ league, showOwner = true }: LeagueCardProps) {
  // Props are READ-ONLY. Never modify props directly.
  // This is like readonly parameters - you can read but not write.
}
```

**Pitfall:** Never do `props.league.name = "New Name"`. Props are immutable.

### State = Private Mutable Fields

```typescript
// C# - Private field that can change
private string _name;
public void SetName(string value) => _name = value;

// React - useState hook
const [name, setName] = useState('');
// name = current value (like reading _name)
// setName = setter function (like SetName method)

// IMPORTANT: Never do name = 'New Value'
// Always use setName('New Value')
```

**Why?** React needs to know when state changes to re-render. Direct assignment bypasses this.

### Event Handlers = Delegates

```typescript
// C# - Event handler
button.Click += HandleClick;
private void HandleClick(object sender, EventArgs e) { }

// React - Event handler
<Button onClick={handleClick}>
const handleClick = () => {
  // Handle the click
};

// With event parameter
const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
  setName(e.target.value);
};
```

### Conditional Rendering = If/Else in Templates

```typescript
// C# Razor - Conditional rendering
@if (User.IsOwner)
{
    <DeleteButton />
}

// React - Conditional rendering (three ways)

// 1. Ternary for either/or
{isOwner ? <OwnerControls /> : <MemberView />}

// 2. && for show/hide (most common)
{isOwner && <DeleteButton />}

// 3. Early return for major conditions
if (isLoading) return <Skeleton />;
if (error) return <Error />;
return <ActualContent />;
```

### List Rendering = LINQ Select + foreach

```typescript
// C# LINQ
leagues.Select(l => new LeagueCard(l)).ToList();

// React - map() returns JSX elements
{leagues.map(league => (
  <LeagueCard key={league.id} league={league} />
))}

// KEY PROP IS CRITICAL
// - React uses key to track which items changed
// - Always use a unique identifier (id), never array index
// - Like a primary key in database
```

**Pitfall:** Missing or wrong `key` causes bugs and poor performance.

---

## Step 1: Create League Card Component

### File: `web/src/components/leagues/league-card.tsx`

```typescript
'use client';

import Link from 'next/link';
import { Users, Calendar, Lock, Globe } from 'lucide-react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import type { LeagueSummaryDto } from '@/types';

interface LeagueCardProps {
  league: LeagueSummaryDto;
}

/**
 * Card displaying league summary in a list
 *
 * Component Structure:
 * - Receives data via props (like method parameters)
 * - Renders UI based on that data
 * - Wraps in Link for navigation (like href in Razor)
 */
export function LeagueCard({ league }: LeagueCardProps) {
  // Format date - same concept as ToString("format") in C#
  const createdDate = new Date(league.createdAt).toLocaleDateString();

  return (
    <Link href={`/leagues/${league.id}`}>
      <Card className="transition-colors hover:bg-muted/50 cursor-pointer">
        <CardHeader className="pb-2">
          <div className="flex items-start justify-between">
            <div>
              <CardTitle className="text-lg">{league.name}</CardTitle>
              <CardDescription>by {league.ownerUsername}</CardDescription>
            </div>
            {/* Conditional badge based on isPublic */}
            <Badge variant={league.isPublic ? 'secondary' : 'outline'}>
              {league.isPublic ? (
                <>
                  <Globe className="h-3 w-3 mr-1" />
                  Public
                </>
              ) : (
                <>
                  <Lock className="h-3 w-3 mr-1" />
                  Private
                </>
              )}
            </Badge>
          </div>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-4 text-sm text-muted-foreground">
            <div className="flex items-center gap-1">
              <Users className="h-4 w-4" />
              <span>{league.memberCount} members</span>
            </div>
            <div className="flex items-center gap-1">
              <Calendar className="h-4 w-4" />
              <span>{createdDate}</span>
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}
```

---

## Step 2: Create League List Component

### File: `web/src/components/leagues/league-list.tsx`

```typescript
'use client';

import { Trophy, Plus } from 'lucide-react';
import { useLeagues } from '@/hooks/use-leagues';
import { LeagueCard } from './league-card';
import { CardGridSkeleton } from '@/components/shared/loading-skeleton';
import { EmptyState } from '@/components/shared/empty-state';
import { ErrorMessage } from '@/components/shared/error-message';
import { Button } from '@/components/ui/button';
import Link from 'next/link';

/**
 * Displays list of user's leagues with loading/error/empty states
 *
 * This demonstrates the typical data fetching pattern:
 * 1. Call useQuery hook (like calling a repository method)
 * 2. Handle loading state (isLoading)
 * 3. Handle error state (isError, error)
 * 4. Handle empty state (data?.length === 0)
 * 5. Render actual data
 *
 * Backend Analogy:
 * This is like a Razor page with @if checks for different states
 */
export function LeagueList() {
  // This hook calls GET /api/leagues under the hood
  // Think of it as: var leagues = await _leagueRepository.GetUserLeaguesAsync();
  const { data: leagues, isLoading, isError, error, refetch } = useLeagues();

  // Loading state - show skeletons while fetching
  if (isLoading) {
    return <CardGridSkeleton count={6} />;
  }

  // Error state - show error with retry option
  if (isError) {
    return (
      <ErrorMessage
        title="Failed to load leagues"
        message={error?.message ?? 'An unexpected error occurred'}
        onRetry={() => refetch()}
      />
    );
  }

  // Empty state - no leagues yet
  if (!leagues || leagues.length === 0) {
    return (
      <EmptyState
        icon={Trophy}
        title="No leagues yet"
        description="Create your first league or join one with an invite code"
        action={{
          label: 'Create League',
          onClick: () => {
            // Will be handled by navigation
          },
        }}
      />
    );
  }

  // Success state - render the list
  return (
    <div className="space-y-4">
      {/* Header with action button */}
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold tracking-tight">Your Leagues</h2>
        <Button asChild>
          <Link href="/leagues/create">
            <Plus className="h-4 w-4 mr-2" />
            Create League
          </Link>
        </Button>
      </div>

      {/* League grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {leagues.map((league) => (
          <LeagueCard key={league.id} league={league} />
        ))}
      </div>
    </div>
  );
}
```

---

## Step 3: Create League Form Component

### File: `web/src/components/leagues/league-form.tsx`

```typescript
'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { useCreateLeague, useUpdateLeague } from '@/hooks/use-leagues';
import type { LeagueDto, CreateLeagueRequest, UpdateLeagueRequest } from '@/types';

interface LeagueFormProps {
  // If provided, we're editing. If not, we're creating.
  // This pattern is like having Create/Update commands share a form
  league?: LeagueDto;
}

/**
 * Form for creating or editing a league
 *
 * CONTROLLED INPUTS EXPLAINED:
 * In React, form inputs can be "controlled" or "uncontrolled".
 *
 * Controlled (recommended):
 * - React state holds the value
 * - Input displays state value
 * - onChange updates state
 * - You always know the current value
 *
 * Backend Analogy:
 * - Like a ViewModel with properties bound to form fields
 * - OnPropertyChanged updates the view
 * - Same concept as MVVM or Blazor two-way binding
 */
export function LeagueForm({ league }: LeagueFormProps) {
  const router = useRouter();
  const isEditing = !!league;

  // Form state - each field has its own state
  // Default to existing values if editing
  const [name, setName] = useState(league?.name ?? '');
  const [description, setDescription] = useState(league?.description ?? '');
  const [isPublic, setIsPublic] = useState(league?.isPublic ?? false);
  const [maxMembers, setMaxMembers] = useState(league?.maxMembers ?? 20);
  const [scoreExactMatch, setScoreExactMatch] = useState(league?.scoreExactMatch ?? 3);
  const [scoreCorrectResult, setScoreCorrectResult] = useState(league?.scoreCorrectResult ?? 1);
  const [bettingDeadlineMinutes, setBettingDeadlineMinutes] = useState(
    league?.bettingDeadlineMinutes ?? 60
  );

  // Validation errors (simple approach)
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Mutations - like command handlers
  const createMutation = useCreateLeague();
  const updateMutation = useUpdateLeague(league?.id ?? '');

  // Use the appropriate mutation based on mode
  const mutation = isEditing ? updateMutation : createMutation;

  // Client-side validation (like FluentValidation, but simpler)
  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!name.trim()) {
      newErrors.name = 'Name is required';
    } else if (name.length < 3) {
      newErrors.name = 'Name must be at least 3 characters';
    } else if (name.length > 100) {
      newErrors.name = 'Name must be less than 100 characters';
    }

    if (maxMembers < 2 || maxMembers > 100) {
      newErrors.maxMembers = 'Must be between 2 and 100';
    }

    if (scoreExactMatch < 1) {
      newErrors.scoreExactMatch = 'Must be at least 1';
    }

    if (scoreCorrectResult < 0) {
      newErrors.scoreCorrectResult = 'Cannot be negative';
    }

    if (bettingDeadlineMinutes < 0) {
      newErrors.bettingDeadlineMinutes = 'Cannot be negative';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Form submission handler
  const handleSubmit = async (e: React.FormEvent) => {
    // Prevent default form submission (page reload)
    e.preventDefault();

    if (!validate()) return;

    // Build request object
    const data: CreateLeagueRequest | UpdateLeagueRequest = {
      name: name.trim(),
      description: description.trim() || undefined,
      isPublic,
      maxMembers,
      scoreExactMatch,
      scoreCorrectResult,
      bettingDeadlineMinutes,
    };

    try {
      await mutation.mutateAsync(data);

      // Show success toast
      toast.success(isEditing ? 'League updated' : 'League created');

      // Navigate back to leagues list
      router.push('/leagues');
    } catch (error) {
      // Error is handled by TanStack Query, but we can show a toast
      const message =
        error instanceof Error ? error.message : 'Something went wrong';
      toast.error(message);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <Card>
        <CardHeader>
          <CardTitle>{isEditing ? 'Edit League' : 'Create League'}</CardTitle>
          <CardDescription>
            {isEditing
              ? 'Update your league settings'
              : 'Set up a new league for you and your friends'}
          </CardDescription>
        </CardHeader>

        <CardContent className="space-y-4">
          {/* Name field */}
          <div className="space-y-2">
            <Label htmlFor="name">League Name *</Label>
            <Input
              id="name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Premier League Predictors"
              aria-invalid={!!errors.name}
            />
            {errors.name && (
              <p className="text-sm text-destructive">{errors.name}</p>
            )}
          </div>

          {/* Description field */}
          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Input
              id="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="A friendly competition among friends"
            />
          </div>

          {/* Public/Private toggle */}
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="isPublic"
              checked={isPublic}
              onChange={(e) => setIsPublic(e.target.checked)}
              className="h-4 w-4 rounded border-gray-300"
            />
            <Label htmlFor="isPublic">Make league public (visible to everyone)</Label>
          </div>

          {/* Scoring settings */}
          <div className="grid gap-4 sm:grid-cols-3">
            <div className="space-y-2">
              <Label htmlFor="maxMembers">Max Members</Label>
              <Input
                id="maxMembers"
                type="number"
                value={maxMembers}
                onChange={(e) => setMaxMembers(Number(e.target.value))}
                min={2}
                max={100}
                aria-invalid={!!errors.maxMembers}
              />
              {errors.maxMembers && (
                <p className="text-sm text-destructive">{errors.maxMembers}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="scoreExactMatch">Points for Exact Match</Label>
              <Input
                id="scoreExactMatch"
                type="number"
                value={scoreExactMatch}
                onChange={(e) => setScoreExactMatch(Number(e.target.value))}
                min={1}
                aria-invalid={!!errors.scoreExactMatch}
              />
              {errors.scoreExactMatch && (
                <p className="text-sm text-destructive">{errors.scoreExactMatch}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="scoreCorrectResult">Points for Correct Result</Label>
              <Input
                id="scoreCorrectResult"
                type="number"
                value={scoreCorrectResult}
                onChange={(e) => setScoreCorrectResult(Number(e.target.value))}
                min={0}
                aria-invalid={!!errors.scoreCorrectResult}
              />
              {errors.scoreCorrectResult && (
                <p className="text-sm text-destructive">{errors.scoreCorrectResult}</p>
              )}
            </div>
          </div>

          {/* Betting deadline */}
          <div className="space-y-2">
            <Label htmlFor="bettingDeadlineMinutes">
              Betting Deadline (minutes before match)
            </Label>
            <Input
              id="bettingDeadlineMinutes"
              type="number"
              value={bettingDeadlineMinutes}
              onChange={(e) => setBettingDeadlineMinutes(Number(e.target.value))}
              min={0}
              aria-invalid={!!errors.bettingDeadlineMinutes}
            />
            <p className="text-sm text-muted-foreground">
              Users must place bets at least this many minutes before kick-off
            </p>
          </div>
        </CardContent>

        <CardFooter className="flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => router.back()}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending
              ? isEditing
                ? 'Saving...'
                : 'Creating...'
              : isEditing
                ? 'Save Changes'
                : 'Create League'}
          </Button>
        </CardFooter>
      </Card>
    </form>
  );
}
```

**Decision Analysis: Form State Management**

| Approach | Why Use | Why Not Here |
|----------|---------|--------------|
| **useState (chosen)** | Simple, no dependencies, good for learning | Verbose for large forms |
| React Hook Form | Less code, better performance | Extra dependency, steeper learning curve |
| Formik | Popular, full-featured | Large bundle, overkill for this size |

**We chose useState because:**
1. You're learning - seeing explicit state helps understand the flow
2. Forms are not complex enough to need a library
3. Later you can refactor to React Hook Form when comfortable

---

## Step 4: Create League Detail Component

### File: `web/src/components/leagues/league-detail.tsx`

```typescript
'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import {
  Trophy,
  Users,
  Settings,
  Copy,
  LogOut,
  Trash2,
  Target,
  Clock,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  useLeague,
  useDeleteLeague,
  useLeaveLeague,
  useRegenerateInviteCode,
} from '@/hooks/use-leagues';
import { useAuthStore } from '@/stores/auth-store';
import { MemberList } from './member-list';
import { InviteShare } from './invite-share';
import { CardSkeleton } from '@/components/shared/loading-skeleton';
import { ErrorMessage } from '@/components/shared/error-message';
import type { LeagueDetailDto } from '@/types';

interface LeagueDetailProps {
  leagueId: string;
}

/**
 * Full league detail view with actions
 *
 * Demonstrates:
 * - Fetching single resource with useQuery
 * - Conditional actions based on user role
 * - Multiple mutations on same page
 * - Optimistic UI updates
 */
export function LeagueDetail({ leagueId }: LeagueDetailProps) {
  const router = useRouter();
  const currentUser = useAuthStore((state) => state.user);

  // Fetch league data
  const { data: league, isLoading, isError, error, refetch } = useLeague(leagueId);

  // Mutations
  const deleteMutation = useDeleteLeague();
  const leaveMutation = useLeaveLeague();
  const regenerateMutation = useRegenerateInviteCode(leagueId);

  // State for invite share modal
  const [showInviteShare, setShowInviteShare] = useState(false);

  // Derive user role
  const isOwner = league?.ownerId === currentUser?.id;
  const isMember = league?.members.some((m) => m.userId === currentUser?.id);

  // Handlers
  const handleCopyInviteCode = () => {
    if (league?.inviteCode) {
      navigator.clipboard.writeText(league.inviteCode);
      toast.success('Invite code copied to clipboard');
    }
  };

  const handleRegenerateCode = async () => {
    try {
      await regenerateMutation.mutateAsync(undefined);
      toast.success('New invite code generated');
    } catch {
      toast.error('Failed to generate new code');
    }
  };

  const handleLeaveLeague = async () => {
    if (!confirm('Are you sure you want to leave this league?')) return;

    try {
      await leaveMutation.mutateAsync(leagueId);
      toast.success('You have left the league');
      router.push('/leagues');
    } catch {
      toast.error('Failed to leave league');
    }
  };

  const handleDeleteLeague = async () => {
    if (!confirm('Are you sure? This will permanently delete the league and all bets.')) {
      return;
    }

    try {
      await deleteMutation.mutateAsync(leagueId);
      toast.success('League deleted');
      router.push('/leagues');
    } catch {
      toast.error('Failed to delete league');
    }
  };

  // Loading state
  if (isLoading) {
    return (
      <div className="space-y-4">
        <CardSkeleton />
        <CardSkeleton />
      </div>
    );
  }

  // Error state
  if (isError || !league) {
    return (
      <ErrorMessage
        title="Failed to load league"
        message={error?.message ?? 'League not found'}
        onRetry={() => refetch()}
      />
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{league.name}</h1>
          {league.description && (
            <p className="text-muted-foreground mt-1">{league.description}</p>
          )}
        </div>

        {/* Actions dropdown */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="icon">
              <Settings className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => setShowInviteShare(true)}>
              <Copy className="h-4 w-4 mr-2" />
              Share Invite
            </DropdownMenuItem>

            {isOwner && (
              <>
                <DropdownMenuItem onClick={() => router.push(`/leagues/${leagueId}/edit`)}>
                  <Settings className="h-4 w-4 mr-2" />
                  Edit Settings
                </DropdownMenuItem>
                <DropdownMenuItem onClick={handleRegenerateCode}>
                  <Copy className="h-4 w-4 mr-2" />
                  Regenerate Invite Code
                </DropdownMenuItem>
              </>
            )}

            <DropdownMenuSeparator />

            {!isOwner && isMember && (
              <DropdownMenuItem
                className="text-destructive"
                onClick={handleLeaveLeague}
              >
                <LogOut className="h-4 w-4 mr-2" />
                Leave League
              </DropdownMenuItem>
            )}

            {isOwner && (
              <DropdownMenuItem
                className="text-destructive"
                onClick={handleDeleteLeague}
              >
                <Trash2 className="h-4 w-4 mr-2" />
                Delete League
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Stats cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <StatsCard
          icon={Users}
          label="Members"
          value={`${league.currentMemberCount}/${league.maxMembers}`}
        />
        <StatsCard
          icon={Target}
          label="Exact Match Points"
          value={league.scoreExactMatch.toString()}
        />
        <StatsCard
          icon={Trophy}
          label="Correct Result Points"
          value={league.scoreCorrectResult.toString()}
        />
        <StatsCard
          icon={Clock}
          label="Betting Deadline"
          value={`${league.bettingDeadlineMinutes} min`}
        />
      </div>

      {/* Quick actions */}
      <div className="flex gap-2">
        <Button onClick={() => router.push(`/leagues/${leagueId}/matches`)}>
          <Target className="h-4 w-4 mr-2" />
          Place Bets
        </Button>
        <Button variant="outline" onClick={() => router.push(`/leagues/${leagueId}/standings`)}>
          <Trophy className="h-4 w-4 mr-2" />
          View Standings
        </Button>
      </div>

      {/* Members list */}
      <MemberList
        members={league.members}
        ownerId={league.ownerId}
        leagueId={leagueId}
        isOwner={isOwner}
      />

      {/* Invite share dialog */}
      {showInviteShare && (
        <InviteShare
          inviteCode={league.inviteCode}
          leagueName={league.name}
          onClose={() => setShowInviteShare(false)}
        />
      )}
    </div>
  );
}

// Helper component for stats cards
function StatsCard({
  icon: Icon,
  label,
  value,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  value: string;
}) {
  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center gap-2">
          <Icon className="h-4 w-4 text-muted-foreground" />
          <span className="text-sm text-muted-foreground">{label}</span>
        </div>
        <p className="text-2xl font-bold mt-1">{value}</p>
      </CardContent>
    </Card>
  );
}
```

---

## Step 5: Create Member List Component

### File: `web/src/components/leagues/member-list.tsx`

```typescript
'use client';

import { toast } from 'sonner';
import { Crown, MoreVertical, UserMinus } from 'lucide-react';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useKickMember } from '@/hooks/use-leagues';
import type { LeagueMemberDto } from '@/types';

interface MemberListProps {
  members: LeagueMemberDto[];
  ownerId: string;
  leagueId: string;
  isOwner: boolean;
}

/**
 * List of league members with kick functionality for owners
 *
 * Demonstrates:
 * - Rendering lists with map()
 * - Conditional rendering based on user role
 * - Inline actions with dropdown menus
 */
export function MemberList({ members, ownerId, leagueId, isOwner }: MemberListProps) {
  const kickMutation = useKickMember(leagueId);

  const handleKick = async (userId: string, username: string) => {
    if (!confirm(`Are you sure you want to remove ${username} from the league?`)) {
      return;
    }

    try {
      await kickMutation.mutateAsync(userId);
      toast.success(`${username} has been removed`);
    } catch {
      toast.error('Failed to remove member');
    }
  };

  // Sort: owner first, then alphabetically
  const sortedMembers = [...members].sort((a, b) => {
    if (a.role === 'Owner') return -1;
    if (b.role === 'Owner') return 1;
    return a.username.localeCompare(b.username);
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Members ({members.length})</CardTitle>
        <CardDescription>People in this league</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-2">
          {sortedMembers.map((member) => (
            <div
              key={member.userId}
              className="flex items-center justify-between p-2 rounded-lg hover:bg-muted/50"
            >
              <div className="flex items-center gap-3">
                {/* Avatar with initials */}
                <Avatar>
                  <AvatarFallback>
                    {member.username.slice(0, 2).toUpperCase()}
                  </AvatarFallback>
                </Avatar>

                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-medium">{member.username}</span>
                    {member.role === 'Owner' && (
                      <Badge variant="secondary" className="gap-1">
                        <Crown className="h-3 w-3" />
                        Owner
                      </Badge>
                    )}
                  </div>
                  <span className="text-sm text-muted-foreground">
                    Joined {new Date(member.joinedAt).toLocaleDateString()}
                  </span>
                </div>
              </div>

              {/* Owner can kick non-owner members */}
              {isOwner && member.userId !== ownerId && (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon">
                      <MoreVertical className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem
                      className="text-destructive"
                      onClick={() => handleKick(member.userId, member.username)}
                    >
                      <UserMinus className="h-4 w-4 mr-2" />
                      Remove from League
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              )}
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
```

---

## Step 6: Create Invite Share Component

### File: `web/src/components/leagues/invite-share.tsx`

```typescript
'use client';

import { Check, Copy, Link as LinkIcon, X } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';

interface InviteShareProps {
  inviteCode: string;
  leagueName: string;
  onClose: () => void;
}

/**
 * Dialog for sharing league invite code
 *
 * Demonstrates:
 * - Dialog/Modal pattern
 * - Clipboard API usage
 * - Visual feedback on actions
 */
export function InviteShare({ inviteCode, leagueName, onClose }: InviteShareProps) {
  const [copied, setCopied] = useState(false);

  // Build invite URL (for future deep linking)
  const inviteUrl = `${window.location.origin}/leagues/join?code=${inviteCode}`;

  const handleCopyCode = async () => {
    await navigator.clipboard.writeText(inviteCode);
    setCopied(true);
    toast.success('Invite code copied!');

    // Reset after 2 seconds
    setTimeout(() => setCopied(false), 2000);
  };

  const handleCopyLink = async () => {
    await navigator.clipboard.writeText(inviteUrl);
    toast.success('Invite link copied!');
  };

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Invite to {leagueName}</DialogTitle>
          <DialogDescription>
            Share this code or link with friends to invite them
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {/* Invite code */}
          <div>
            <label className="text-sm font-medium">Invite Code</label>
            <div className="flex gap-2 mt-1">
              <Input
                value={inviteCode}
                readOnly
                className="font-mono text-lg tracking-wider"
              />
              <Button onClick={handleCopyCode} variant="outline">
                {copied ? (
                  <Check className="h-4 w-4" />
                ) : (
                  <Copy className="h-4 w-4" />
                )}
              </Button>
            </div>
          </div>

          {/* Invite link */}
          <div>
            <label className="text-sm font-medium">Invite Link</label>
            <div className="flex gap-2 mt-1">
              <Input value={inviteUrl} readOnly className="text-sm" />
              <Button onClick={handleCopyLink} variant="outline">
                <LinkIcon className="h-4 w-4" />
              </Button>
            </div>
          </div>

          <p className="text-sm text-muted-foreground">
            Anyone with this code can join your league
          </p>
        </div>
      </DialogContent>
    </Dialog>
  );
}
```

---

## Step 7: Create Join League Form

### File: `web/src/components/leagues/join-league-form.tsx`

```typescript
'use client';

import { useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { useJoinLeague } from '@/hooks/use-leagues';

/**
 * Form for joining a league via invite code
 *
 * Demonstrates:
 * - Reading URL search params (like query string in .NET)
 * - Prefilling form from URL params
 * - Navigation after success
 */
export function JoinLeagueForm() {
  const router = useRouter();
  const searchParams = useSearchParams();

  // Pre-fill from URL if code is in query string
  // URL: /leagues/join?code=ABC123
  const [code, setCode] = useState(searchParams.get('code') ?? '');
  const [leagueId, setLeagueId] = useState('');
  const [error, setError] = useState('');

  // We need leagueId to join, but user only has invite code
  // This is a two-step process: enter code → identify league → join
  // For simplicity, we'll expect leagueId in URL or have user paste full invite link

  // Alternative: Create a backend endpoint that accepts just the invite code
  // For now, we'll use a combined approach

  const joinMutation = useJoinLeague(leagueId);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!code.trim()) {
      setError('Please enter an invite code');
      return;
    }

    try {
      await joinMutation.mutateAsync({ inviteCode: code.trim() });
      toast.success('Successfully joined the league!');
      router.push('/leagues');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Invalid invite code';
      setError(message);
      toast.error(message);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <Card className="max-w-md mx-auto">
        <CardHeader>
          <CardTitle>Join a League</CardTitle>
          <CardDescription>
            Enter the invite code you received to join a league
          </CardDescription>
        </CardHeader>

        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="code">Invite Code</Label>
            <Input
              id="code"
              value={code}
              onChange={(e) => setCode(e.target.value.toUpperCase())}
              placeholder="ABC123"
              className="font-mono text-lg tracking-wider"
              aria-invalid={!!error}
            />
            {error && (
              <p className="text-sm text-destructive">{error}</p>
            )}
          </div>
        </CardContent>

        <CardFooter className="flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => router.push('/leagues')}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={joinMutation.isPending}>
            {joinMutation.isPending ? 'Joining...' : 'Join League'}
          </Button>
        </CardFooter>
      </Card>
    </form>
  );
}
```

---

## Step 8: Create Page Files

### File: `web/src/app/(protected)/leagues/page.tsx`

```typescript
import { ProtectedRoute } from '@/components/auth/protected-route';
import { LeagueList } from '@/components/leagues/league-list';

export default function LeaguesPage() {
  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
        <div className="mx-auto max-w-6xl">
          <LeagueList />
        </div>
      </div>
    </ProtectedRoute>
  );
}
```

### File: `web/src/app/(protected)/leagues/[id]/page.tsx`

```typescript
import { ProtectedRoute } from '@/components/auth/protected-route';
import { LeagueDetail } from '@/components/leagues/league-detail';

interface PageProps {
  params: Promise<{ id: string }>;
}

/**
 * Dynamic route for league detail
 *
 * Next.js 15 note: params is now a Promise
 * The [id] in the folder name becomes params.id
 */
export default async function LeagueDetailPage({ params }: PageProps) {
  const { id } = await params;

  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
        <div className="mx-auto max-w-4xl">
          <LeagueDetail leagueId={id} />
        </div>
      </div>
    </ProtectedRoute>
  );
}
```

### File: `web/src/app/(protected)/leagues/create/page.tsx`

```typescript
import { ProtectedRoute } from '@/components/auth/protected-route';
import { LeagueForm } from '@/components/leagues/league-form';

export default function CreateLeaguePage() {
  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
        <div className="mx-auto max-w-2xl">
          <LeagueForm />
        </div>
      </div>
    </ProtectedRoute>
  );
}
```

### File: `web/src/app/(protected)/leagues/join/page.tsx`

```typescript
import { Suspense } from 'react';
import { ProtectedRoute } from '@/components/auth/protected-route';
import { JoinLeagueForm } from '@/components/leagues/join-league-form';
import { CardSkeleton } from '@/components/shared/loading-skeleton';

export default function JoinLeaguePage() {
  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4 flex items-center">
        {/* Suspense needed for useSearchParams */}
        <Suspense fallback={<CardSkeleton />}>
          <JoinLeagueForm />
        </Suspense>
      </div>
    </ProtectedRoute>
  );
}
```

---

## Step 9: Add Required shadcn/ui Components

```bash
cd web
npx shadcn@latest add dialog
npx shadcn@latest add dropdown-menu
npx shadcn@latest add badge
npx shadcn@latest add avatar
npx shadcn@latest add separator
```

---

## Step 10: Update Dashboard with League Link

Update `web/src/app/(protected)/dashboard/page.tsx` to link to leagues:

```typescript
// Add to the leagues card:
<CardContent>
  <Button asChild className="w-full">
    <Link href="/leagues">View Leagues</Link>
  </Button>
</CardContent>
```

---

## Verification Checklist

After completing Phase 6.2:

- [ ] Leagues list page shows user's leagues
- [ ] Create league form works and validates input
- [ ] League detail page shows members and settings
- [ ] Invite code copy works (clipboard)
- [ ] Owner can regenerate invite code
- [ ] Owner can kick members
- [ ] Non-owner can leave league
- [ ] Owner can delete league
- [ ] Join league with invite code works
- [ ] Empty state shows when no leagues
- [ ] Loading skeletons display during fetch
- [ ] Error messages appear on API failures
- [ ] `npm run build` passes

---

## Key Learnings from This Phase

1. **Props are immutable** - Like readonly method parameters
2. **State triggers re-renders** - Use setters, never assign directly
3. **map() for lists** - Always include unique `key` prop
4. **Conditional rendering** - Use `&&` for show/hide, ternary for either/or
5. **Form handling** - Controlled inputs with value + onChange
6. **Mutations** - Use useMutation for POST/PUT/DELETE, invalidate queries on success
7. **Early returns** - Handle loading/error before rendering content

---

## Next Step

Proceed to **Phase 6.3: Betting System UI** (`phase-6.3-betting.md`)
