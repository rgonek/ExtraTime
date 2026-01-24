# Phase 6 Detailed Plan: Frontend Implementation & Polish

## For: Senior .NET Developer Learning Frontend Architecture

---

# Part 1: The .NET Mental Model

Before writing any code, let's establish how React/Next.js concepts map to patterns you already know.

## Core Concept Translations

### 1. React Context ≈ Dependency Injection (Program.cs)

**In ASP.NET Core:**
```csharp
// Program.cs - Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<ICache, RedisCache>();

// Controller - Inject and use
public class UsersController(IUserService userService) { }
```

**In React (Context + Providers):**
```tsx
// providers.tsx - Register "services"
<QueryClientProvider client={queryClient}>
  <ThemeProvider>
    <AuthProvider>
      {children}
    </AuthProvider>
  </ThemeProvider>
</QueryClientProvider>

// Component - "Inject" and use
function Dashboard() {
  const { user } = useAuth();  // Like constructor injection
  const theme = useTheme();    // Another "service"
}
```

**Key Insight:** React's `Provider` wrapping is like `.AddScoped()` registration. The `useXxx()` hooks are like constructor injection - they pull from the "container" (React's Context tree).

---

### 2. TanStack Query ≈ Repository Pattern + Caching Layer

**In ASP.NET Core:**
```csharp
public class LeagueRepository(IDbContext db, IMemoryCache cache)
{
    public async Task<League?> GetByIdAsync(Guid id)
    {
        return await cache.GetOrCreateAsync($"league:{id}", async entry => {
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);
            return await db.Leagues.FindAsync(id);
        });
    }
}
```

**In React (TanStack Query):**
```tsx
// This IS your cached repository
const { data: league, isLoading, error } = useQuery({
  queryKey: ['league', leagueId],        // Cache key (like $"league:{id}")
  queryFn: () => apiClient.get(`/leagues/${leagueId}`),
  staleTime: 5 * 60 * 1000,              // Like SlidingExpiration
});
```

**Key Insight:** TanStack Query is NOT just "fetch with hooks." It's a full caching layer with:
- Automatic background refetching (like cache invalidation)
- Deduplication (multiple components requesting same data = 1 API call)
- Optimistic updates (like EF change tracking but for UI)

---

### 3. Zustand ≈ Singleton Service with INotifyPropertyChanged

**In WPF/MAUI (MVVM):**
```csharp
public class AuthService : INotifyPropertyChanged
{
    private User? _user;
    public User? User
    {
        get => _user;
        set { _user = value; OnPropertyChanged(); }
    }

    public void Login(User user) { User = user; }
    public void Logout() { User = null; }
}
```

**In React (Zustand):**
```tsx
// This IS your singleton service
export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  setUser: (user) => set({ user }),      // Like property setter + OnPropertyChanged
  logout: () => set({ user: null }),
}));

// Component subscribes (like data binding)
function Header() {
  const user = useAuthStore((s) => s.user);  // Auto re-renders on change
}
```

**Key Insight:** Zustand is a singleton state container. When you call `set()`, ALL components subscribed to that slice automatically re-render. It's reactive binding without explicit `INotifyPropertyChanged`.

---

### 4. Next.js Middleware ≈ ASP.NET Core Middleware

**In ASP.NET Core:**
```csharp
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) => {
    if (!context.User.Identity.IsAuthenticated &&
        context.Request.Path.StartsWithSegments("/dashboard"))
    {
        context.Response.Redirect("/login");
        return;
    }
    await next();
});
```

**In Next.js (middleware.ts):**
```tsx
export function middleware(request: NextRequest) {
  const token = request.cookies.get('token');

  if (!token && request.nextUrl.pathname.startsWith('/dashboard')) {
    return NextResponse.redirect(new URL('/login', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/dashboard/:path*', '/leagues/:path*'],
};
```

**Key Insight:** Same pipeline concept. Runs BEFORE the page renders. Use for auth guards, redirects, header injection.

---

### 5. React Component Lifecycle ≈ NOT the Request Lifecycle

**THE BACKEND TRAP:**

In ASP.NET, a request flows linearly:
```
Request → Middleware → Controller → Service → Repository → Response
         (one direction, one time, then done)
```

In React, components PERSIST and RE-RENDER:
```
Mount → Render → User clicks → State changes → Re-render → Re-render → ...
       (lives in browser, re-executes on EVERY state change)
```

**Critical Difference:**
```csharp
// ASP.NET - This runs ONCE per request
public IActionResult GetUser()
{
    var user = _userService.GetCurrent();  // Called once
    _logger.Log("Fetched user");           // Logged once
    return Ok(user);
}
```

```tsx
// React - This runs on EVERY render (could be 50 times!)
function UserProfile() {
  console.log("Rendered!");  // Logs on EVERY state change
  const user = useAuthStore(s => s.user);  // Fine - hooks are designed for this

  // WRONG: This creates a new fetch on every render
  fetch('/api/user').then(...)  // BAD - infinite loop potential

  // CORRECT: Use TanStack Query (handles render cycles)
  const { data } = useQuery({ queryKey: ['user'], queryFn: fetchUser });

  return <div>{user?.name}</div>;
}
```

---

### 6. Props ≈ Method Parameters / DTOs

**In C#:**
```csharp
public record UserCardProps(string Name, string AvatarUrl, int Points);

public string RenderUserCard(UserCardProps props)
{
    return $"<div>{props.Name} - {props.Points}pts</div>";
}
```

**In React:**
```tsx
interface UserCardProps {
  name: string;
  avatarUrl: string;
  points: number;
}

function UserCard({ name, avatarUrl, points }: UserCardProps) {
  return <div>{name} - {points}pts</div>;
}

// Usage (like calling a method with named parameters)
<UserCard name="John" avatarUrl="/img.png" points={150} />
```

**Key Insight:** Components are functions. Props are parameters. TypeScript interfaces are your DTOs.

---

# Part 2: Implementation Sub-Phases

## Phase 6.1: Type Foundation & API Hooks

### Goal
Establish type-safe DTOs and reusable API hooks (your "frontend repositories")

### The .NET Mental Model
This is like creating your `Application/DTOs/` folder and `Infrastructure/Repositories/` - defining the contracts before implementation.

### Implementation

**Step 6.1.1: Expand Type Definitions**

```typescript
// web/src/types/index.ts

// ===== League Types (matches backend DTOs) =====
export interface LeagueDto {
  id: string;
  name: string;
  description: string | null;
  status: 'Active' | 'Archived';
  inviteCode: string;
  ownerId: string;
  ownerUsername: string;
  memberCount: number;
  createdAt: string;
}

export interface LeagueDetailDto extends LeagueDto {
  members: LeagueMemberDto[];
}

export interface LeagueMemberDto {
  userId: string;
  username: string;
  role: 'Owner' | 'Member';
  joinedAt: string;
  totalPoints: number;
}

export interface CreateLeagueRequest {
  name: string;
  description?: string;
}

// ===== Match Types =====
export interface MatchDto {
  id: string;
  competitionId: string;
  competitionName: string;
  homeTeamName: string;
  awayTeamName: string;
  homeTeamCrest: string | null;
  awayTeamCrest: string | null;
  matchDate: string;
  status: 'Scheduled' | 'Live' | 'Finished' | 'Postponed' | 'Cancelled';
  homeScore: number | null;
  awayScore: number | null;
}

// ===== Betting Types =====
export interface BetDto {
  id: string;
  matchId: string;
  predictedHomeScore: number;
  predictedAwayScore: number;
  pointsEarned: number | null;
  createdAt: string;
}

export interface PlaceBetRequest {
  matchId: string;
  predictedHomeScore: number;
  predictedAwayScore: number;
}

export interface MatchBetsDto {
  matchId: string;
  bets: UserBetDto[];
}

export interface UserBetDto {
  oderId: string;
  username: string;
  predictedHomeScore: number;
  predictedAwayScore: number;
  pointsEarned: number | null;
}

// ===== Standings Types =====
export interface StandingDto {
  rank: number;
  userId: string;
  username: string;
  totalPoints: number;
  totalBets: number;
  perfectScores: number;
  correctOutcomes: number;
}

export interface UserStatsDto {
  totalPoints: number;
  totalBets: number;
  perfectScores: number;
  correctOutcomes: number;
  currentStreak: number;
  bestStreak: number;
  averagePoints: number;
}
```

**Verification:** TypeScript compiles without errors. Import types in a component - no red squiggles.

---

**Step 6.1.2: Create API Hooks (Your "Repositories")**

```typescript
// web/src/hooks/use-leagues.ts

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { LeagueDto, LeagueDetailDto, CreateLeagueRequest } from '@/types';

// Query Keys - like cache key constants
export const leagueKeys = {
  all: ['leagues'] as const,
  detail: (id: string) => ['leagues', id] as const,
  standings: (id: string) => ['leagues', id, 'standings'] as const,
};

// GET /api/leagues - List user's leagues
export function useLeagues() {
  return useQuery({
    queryKey: leagueKeys.all,
    queryFn: async () => {
      const response = await apiClient.get<LeagueDto[]>('/leagues');
      return response;
    },
  });
}

// GET /api/leagues/{id} - League detail
export function useLeague(leagueId: string) {
  return useQuery({
    queryKey: leagueKeys.detail(leagueId),
    queryFn: async () => {
      const response = await apiClient.get<LeagueDetailDto>(`/leagues/${leagueId}`);
      return response;
    },
    enabled: !!leagueId,  // Don't fetch if no ID (like a null check)
  });
}

// POST /api/leagues - Create league
export function useCreateLeague() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: CreateLeagueRequest) => {
      return apiClient.post<LeagueDto>('/leagues', data);
    },
    onSuccess: () => {
      // Invalidate cache (like clearing IMemoryCache)
      queryClient.invalidateQueries({ queryKey: leagueKeys.all });
    },
  });
}

// POST /api/leagues/{id}/join - Join league
export function useJoinLeague() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ leagueId, inviteCode }: { leagueId: string; inviteCode: string }) => {
      return apiClient.post<LeagueDto>(`/leagues/${leagueId}/join`, { inviteCode });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: leagueKeys.all });
    },
  });
}
```

**Verification:**
- Import `useLeagues()` in dashboard, check React Query DevTools shows the query
- Network tab shows single request even if multiple components use same hook

---

**Step 6.1.3: Create Betting Hooks**

```typescript
// web/src/hooks/use-bets.ts

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { BetDto, PlaceBetRequest, StandingDto, MatchBetsDto } from '@/types';
import { leagueKeys } from './use-leagues';

export const betKeys = {
  myBets: (leagueId: string) => ['leagues', leagueId, 'bets', 'my'] as const,
  matchBets: (leagueId: string, matchId: string) =>
    ['leagues', leagueId, 'matches', matchId, 'bets'] as const,
  standings: (leagueId: string) => leagueKeys.standings(leagueId),
};

// GET /api/leagues/{leagueId}/bets/my
export function useMyBets(leagueId: string) {
  return useQuery({
    queryKey: betKeys.myBets(leagueId),
    queryFn: () => apiClient.get<BetDto[]>(`/leagues/${leagueId}/bets/my`),
    enabled: !!leagueId,
  });
}

// GET /api/leagues/{leagueId}/standings
export function useStandings(leagueId: string) {
  return useQuery({
    queryKey: betKeys.standings(leagueId),
    queryFn: () => apiClient.get<StandingDto[]>(`/leagues/${leagueId}/standings`),
    enabled: !!leagueId,
  });
}

// POST /api/leagues/{leagueId}/bets - Place bet with optimistic update
export function usePlaceBet(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: PlaceBetRequest) =>
      apiClient.post<BetDto>(`/leagues/${leagueId}/bets`, data),

    // Optimistic update - update UI before server responds
    onMutate: async (newBet) => {
      await queryClient.cancelQueries({ queryKey: betKeys.myBets(leagueId) });

      const previousBets = queryClient.getQueryData<BetDto[]>(betKeys.myBets(leagueId));

      // Optimistically add the new bet
      queryClient.setQueryData<BetDto[]>(betKeys.myBets(leagueId), (old) => [
        ...(old || []),
        { ...newBet, id: 'temp-' + Date.now(), pointsEarned: null, createdAt: new Date().toISOString() },
      ]);

      return { previousBets };
    },

    // Rollback on error
    onError: (_err, _newBet, context) => {
      if (context?.previousBets) {
        queryClient.setQueryData(betKeys.myBets(leagueId), context.previousBets);
      }
    },

    // Sync with server
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: betKeys.myBets(leagueId) });
    },
  });
}
```

**Senior Dev Analysis - Optimistic Updates:**

| Aspect | Analysis |
|--------|----------|
| **Why?** | UX requirement - betting feels instant. Users shouldn't wait 200ms for UI feedback |
| **.NET Equivalent** | Like EF's change tracker - you modify local state, then `SaveChanges()` syncs with DB. If save fails, you'd rollback |
| **Backend Trap** | Don't think of this as "lying to the user." Think of it as "optimistic concurrency" - assume success, handle conflicts |

---

**Step 6.1.4: Create Matches Hook**

```typescript
// web/src/hooks/use-matches.ts

import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { MatchDto } from '@/types';

export const matchKeys = {
  all: ['matches'] as const,
  list: (filters: MatchFilters) => ['matches', filters] as const,
  detail: (id: string) => ['matches', id] as const,
};

interface MatchFilters {
  competitionId?: string;
  dateFrom?: string;
  dateTo?: string;
  status?: string;
}

export function useMatches(filters: MatchFilters = {}) {
  return useQuery({
    queryKey: matchKeys.list(filters),
    queryFn: async () => {
      const params = new URLSearchParams();
      if (filters.competitionId) params.append('competitionId', filters.competitionId);
      if (filters.dateFrom) params.append('dateFrom', filters.dateFrom);
      if (filters.dateTo) params.append('dateTo', filters.dateTo);
      if (filters.status) params.append('status', filters.status);

      const url = `/matches${params.toString() ? '?' + params.toString() : ''}`;
      return apiClient.get<MatchDto[]>(url);
    },
  });
}
```

**Verification Checklist for 6.1:**
- [ ] All types compile without errors
- [ ] `useLeagues()` returns data in React Query DevTools
- [ ] `useMatches()` shows matches in DevTools
- [ ] Network tab shows proper API calls with auth headers
- [ ] Multiple components using same hook = single network request (deduplication)

---

## Phase 6.2: League Management UI

### Goal
Complete CRUD UI for leagues: list, create, view details, join, leave

### The .NET Mental Model
This is like building your Razor Pages or Blazor components - consuming the repositories (hooks) you just created.

### Implementation

**Step 6.2.1: League List Component**

```tsx
// web/src/components/leagues/league-list.tsx

'use client';

import { useLeagues } from '@/hooks/use-leagues';
import { LeagueCard } from './league-card';
import { LeagueCardSkeleton } from './league-card-skeleton';
import { CreateLeagueDialog } from './create-league-dialog';
import { Button } from '@/components/ui/button';
import { Plus } from 'lucide-react';

export function LeagueList() {
  const { data: leagues, isLoading, error } = useLeagues();

  if (error) {
    return (
      <div className="text-center py-12">
        <p className="text-destructive">Failed to load leagues</p>
        <p className="text-sm text-muted-foreground">{error.message}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold">My Leagues</h2>
        <CreateLeagueDialog>
          <Button>
            <Plus className="h-4 w-4 mr-2" />
            Create League
          </Button>
        </CreateLeagueDialog>
      </div>

      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <LeagueCardSkeleton key={i} />
          ))}
        </div>
      ) : leagues?.length === 0 ? (
        <EmptyLeaguesState />
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {leagues?.map((league) => (
            <LeagueCard key={league.id} league={league} />
          ))}
        </div>
      )}
    </div>
  );
}

function EmptyLeaguesState() {
  return (
    <div className="text-center py-12 border-2 border-dashed rounded-lg">
      <h3 className="text-lg font-medium">No leagues yet</h3>
      <p className="text-muted-foreground mt-1">
        Create a league or join one with an invite code
      </p>
    </div>
  );
}
```

**Senior Dev Analysis - Loading States:**

| Aspect | Analysis |
|--------|----------|
| **Why?** | UX requirement - users need feedback during async operations |
| **.NET Equivalent** | Like a Blazor `@if (isLoading)` block, but the state is managed by TanStack Query |
| **Backend Trap** | Don't check `isLoading` and `data` separately thinking they're independent. TanStack Query manages the state machine: `isLoading → isSuccess/isError` |

---

**Step 6.2.2: League Card Component**

```tsx
// web/src/components/leagues/league-card.tsx

import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Users, Trophy } from 'lucide-react';
import type { LeagueDto } from '@/types';

interface LeagueCardProps {
  league: LeagueDto;
}

export function LeagueCard({ league }: LeagueCardProps) {
  return (
    <Link href={`/leagues/${league.id}`}>
      <Card className="hover:shadow-md transition-shadow cursor-pointer">
        <CardHeader className="pb-2">
          <div className="flex items-start justify-between">
            <CardTitle className="text-lg">{league.name}</CardTitle>
            <Badge variant={league.status === 'Active' ? 'default' : 'secondary'}>
              {league.status}
            </Badge>
          </div>
        </CardHeader>
        <CardContent>
          {league.description && (
            <p className="text-sm text-muted-foreground mb-4 line-clamp-2">
              {league.description}
            </p>
          )}
          <div className="flex items-center gap-4 text-sm text-muted-foreground">
            <div className="flex items-center gap-1">
              <Users className="h-4 w-4" />
              <span>{league.memberCount} members</span>
            </div>
            <div className="flex items-center gap-1">
              <Trophy className="h-4 w-4" />
              <span>Owner: {league.ownerUsername}</span>
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}
```

---

**Step 6.2.3: Create League Dialog (Modal)**

```tsx
// web/src/components/leagues/create-league-dialog.tsx

'use client';

import { useState } from 'react';
import { useCreateLeague } from '@/hooks/use-leagues';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { toast } from 'sonner';

interface CreateLeagueDialogProps {
  children: React.ReactNode;
}

export function CreateLeagueDialog({ children }: CreateLeagueDialogProps) {
  const [open, setOpen] = useState(false);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');

  const createLeague = useCreateLeague();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      await createLeague.mutateAsync({ name, description: description || undefined });
      toast.success('League created successfully!');
      setOpen(false);
      setName('');
      setDescription('');
    } catch (error) {
      toast.error('Failed to create league');
    }
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>{children}</DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create New League</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="name">League Name</Label>
            <Input
              id="name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Premier League Predictions"
              required
              minLength={3}
              maxLength={100}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="description">Description (optional)</Label>
            <Textarea
              id="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="A league for friends to predict match outcomes..."
              maxLength={500}
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => setOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={createLeague.isPending}>
              {createLeague.isPending ? 'Creating...' : 'Create League'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
```

**Senior Dev Analysis - Form State:**

| Aspect | Analysis |
|--------|----------|
| **Why?** | React forms are "controlled" - state lives in React, not in DOM |
| **.NET Equivalent** | Like Blazor's `@bind` but explicit. Each `onChange` is like a property setter |
| **Backend Trap** | Don't try to read `document.getElementById('name').value` - that's the jQuery/JS way. React owns the state, not the DOM |

---

**Step 6.2.4: League Detail Page**

```tsx
// web/src/app/(protected)/leagues/[id]/page.tsx

'use client';

import { useParams } from 'next/navigation';
import { useLeague } from '@/hooks/use-leagues';
import { useStandings } from '@/hooks/use-bets';
import { MemberList } from '@/components/leagues/member-list';
import { LeagueStandings } from '@/components/leagues/league-standings';
import { InviteCodeCard } from '@/components/leagues/invite-code-card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Skeleton } from '@/components/ui/skeleton';

export default function LeagueDetailPage() {
  const params = useParams();
  const leagueId = params.id as string;

  const { data: league, isLoading: leagueLoading } = useLeague(leagueId);
  const { data: standings, isLoading: standingsLoading } = useStandings(leagueId);

  if (leagueLoading) {
    return <LeagueDetailSkeleton />;
  }

  if (!league) {
    return <div>League not found</div>;
  }

  return (
    <div className="container py-6 space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl font-bold">{league.name}</h1>
          {league.description && (
            <p className="text-muted-foreground mt-1">{league.description}</p>
          )}
        </div>
        <InviteCodeCard inviteCode={league.inviteCode} leagueId={league.id} />
      </div>

      <Tabs defaultValue="standings">
        <TabsList>
          <TabsTrigger value="standings">Standings</TabsTrigger>
          <TabsTrigger value="members">Members ({league.memberCount})</TabsTrigger>
          <TabsTrigger value="matches">Matches</TabsTrigger>
        </TabsList>

        <TabsContent value="standings" className="mt-4">
          <LeagueStandings
            standings={standings || []}
            isLoading={standingsLoading}
          />
        </TabsContent>

        <TabsContent value="members" className="mt-4">
          <MemberList
            members={league.members}
            ownerId={league.ownerId}
            leagueId={league.id}
          />
        </TabsContent>

        <TabsContent value="matches" className="mt-4">
          {/* Will be implemented in Phase 6.3 */}
          <p>Match betting coming next...</p>
        </TabsContent>
      </Tabs>
    </div>
  );
}

function LeagueDetailSkeleton() {
  return (
    <div className="container py-6 space-y-6">
      <Skeleton className="h-10 w-64" />
      <Skeleton className="h-4 w-96" />
      <Skeleton className="h-[400px] w-full" />
    </div>
  );
}
```

**Verification Checklist for 6.2:**
- [ ] League list shows all user's leagues
- [ ] Empty state displays when no leagues
- [ ] Create league dialog opens and submits
- [ ] New league appears in list after creation (cache invalidation works)
- [ ] League detail page loads with tabs
- [ ] Invite code is visible and copyable

---

## Phase 6.3: Match & Betting UI

### Goal
Display matches, allow bet placement, show user's bets

### The .NET Mental Model
This is your core business feature - like building the order entry form in an e-commerce app. The betting form is like a shopping cart that submits to an order endpoint.

### Implementation

**Step 6.3.1: Match List Component**

```tsx
// web/src/components/matches/match-list.tsx

'use client';

import { useMatches } from '@/hooks/use-matches';
import { MatchCard } from './match-card';
import { MatchCardSkeleton } from './match-card-skeleton';
import { format, parseISO, isToday, isTomorrow, addDays } from 'date-fns';

interface MatchListProps {
  leagueId?: string;  // If provided, shows betting UI
}

export function MatchList({ leagueId }: MatchListProps) {
  const today = format(new Date(), 'yyyy-MM-dd');
  const weekFromNow = format(addDays(new Date(), 7), 'yyyy-MM-dd');

  const { data: matches, isLoading } = useMatches({
    dateFrom: today,
    dateTo: weekFromNow,
    status: 'Scheduled',
  });

  if (isLoading) {
    return (
      <div className="space-y-4">
        {Array.from({ length: 5 }).map((_, i) => (
          <MatchCardSkeleton key={i} />
        ))}
      </div>
    );
  }

  // Group matches by date
  const groupedMatches = groupMatchesByDate(matches || []);

  return (
    <div className="space-y-8">
      {Object.entries(groupedMatches).map(([dateKey, dayMatches]) => (
        <div key={dateKey}>
          <h3 className="text-lg font-semibold mb-4 sticky top-0 bg-background py-2">
            {formatDateHeader(dateKey)}
          </h3>
          <div className="space-y-3">
            {dayMatches.map((match) => (
              <MatchCard
                key={match.id}
                match={match}
                leagueId={leagueId}
              />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

function groupMatchesByDate(matches: MatchDto[]) {
  return matches.reduce((acc, match) => {
    const date = format(parseISO(match.matchDate), 'yyyy-MM-dd');
    if (!acc[date]) acc[date] = [];
    acc[date].push(match);
    return acc;
  }, {} as Record<string, MatchDto[]>);
}

function formatDateHeader(dateStr: string) {
  const date = parseISO(dateStr);
  if (isToday(date)) return 'Today';
  if (isTomorrow(date)) return 'Tomorrow';
  return format(date, 'EEEE, MMMM d');
}
```

---

**Step 6.3.2: Match Card with Betting**

```tsx
// web/src/components/matches/match-card.tsx

'use client';

import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { usePlaceBet, useMyBets } from '@/hooks/use-bets';
import { toast } from 'sonner';
import { format, parseISO } from 'date-fns';
import type { MatchDto } from '@/types';

interface MatchCardProps {
  match: MatchDto;
  leagueId?: string;
}

export function MatchCard({ match, leagueId }: MatchCardProps) {
  const [homeScore, setHomeScore] = useState<string>('');
  const [awayScore, setAwayScore] = useState<string>('');
  const [isEditing, setIsEditing] = useState(false);

  const { data: myBets } = useMyBets(leagueId || '');
  const placeBet = usePlaceBet(leagueId || '');

  // Find existing bet for this match
  const existingBet = myBets?.find(b => b.matchId === match.id);

  const handlePlaceBet = async () => {
    if (!leagueId) return;

    const home = parseInt(homeScore);
    const away = parseInt(awayScore);

    if (isNaN(home) || isNaN(away) || home < 0 || away < 0) {
      toast.error('Please enter valid scores');
      return;
    }

    try {
      await placeBet.mutateAsync({
        matchId: match.id,
        predictedHomeScore: home,
        predictedAwayScore: away,
      });
      toast.success('Bet placed!');
      setIsEditing(false);
    } catch (error) {
      toast.error('Failed to place bet');
    }
  };

  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          {/* Teams */}
          <div className="flex-1 space-y-2">
            <TeamRow
              name={match.homeTeamName}
              crest={match.homeTeamCrest}
              isHome
            />
            <TeamRow
              name={match.awayTeamName}
              crest={match.awayTeamCrest}
            />
          </div>

          {/* Score / Betting Input */}
          <div className="flex items-center gap-2">
            {leagueId && !existingBet && (
              <>
                <Input
                  type="number"
                  min="0"
                  max="20"
                  className="w-14 text-center"
                  value={homeScore}
                  onChange={(e) => setHomeScore(e.target.value)}
                  placeholder="-"
                />
                <span className="text-muted-foreground">:</span>
                <Input
                  type="number"
                  min="0"
                  max="20"
                  className="w-14 text-center"
                  value={awayScore}
                  onChange={(e) => setAwayScore(e.target.value)}
                  placeholder="-"
                />
                <Button
                  size="sm"
                  onClick={handlePlaceBet}
                  disabled={placeBet.isPending}
                >
                  Bet
                </Button>
              </>
            )}

            {existingBet && (
              <div className="text-center">
                <div className="text-lg font-bold">
                  {existingBet.predictedHomeScore} : {existingBet.predictedAwayScore}
                </div>
                <div className="text-xs text-muted-foreground">Your bet</div>
              </div>
            )}
          </div>

          {/* Match Time */}
          <div className="text-right text-sm text-muted-foreground ml-4">
            <div>{format(parseISO(match.matchDate), 'HH:mm')}</div>
            <div className="text-xs">{match.competitionName}</div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function TeamRow({ name, crest, isHome }: { name: string; crest?: string | null; isHome?: boolean }) {
  return (
    <div className="flex items-center gap-2">
      {crest ? (
        <img src={crest} alt={name} className="w-6 h-6 object-contain" />
      ) : (
        <div className="w-6 h-6 bg-muted rounded-full" />
      )}
      <span className={isHome ? 'font-medium' : ''}>{name}</span>
    </div>
  );
}
```

**Senior Dev Analysis - Component State vs Server State:**

| Aspect | Analysis |
|--------|----------|
| **Why?** | `homeScore`/`awayScore` are form inputs (ephemeral). `myBets` is server state (persisted). Different lifetimes = different state management |
| **.NET Equivalent** | Form state is like a ViewModel's bound properties. Server state is like the database record. You don't persist every keystroke |
| **Backend Trap** | Don't put form values in Zustand/global state. That's over-engineering. Local `useState` is perfect for ephemeral form data |

---

**Verification Checklist for 6.3:**
- [ ] Matches display grouped by date
- [ ] Bet input fields appear when viewing within a league context
- [ ] Submitting bet shows optimistic update immediately
- [ ] Existing bet displays instead of input fields
- [ ] Toast notifications appear for success/error

---

## Phase 6.4: Leaderboard & Standings

### Goal
Display ranked leaderboard with visual hierarchy, stats, and animations

### The .NET Mental Model
This is your reporting/dashboard view - consuming aggregated data from your Standings endpoint and presenting it with visual polish.

### Implementation

**Step 6.4.1: Standings Table Component**

```tsx
// web/src/components/leagues/league-standings.tsx

'use client';

import { motion } from 'framer-motion';
import { Card } from '@/components/ui/card';
import { Trophy, Medal, Award } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { StandingDto } from '@/types';

interface LeagueStandingsProps {
  standings: StandingDto[];
  isLoading: boolean;
}

export function LeagueStandings({ standings, isLoading }: LeagueStandingsProps) {
  if (isLoading) {
    return <StandingsSkeleton />;
  }

  if (standings.length === 0) {
    return (
      <div className="text-center py-12 text-muted-foreground">
        No standings yet. Place some bets to get started!
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {standings.map((standing, index) => (
        <motion.div
          key={standing.userId}
          initial={{ opacity: 0, x: -20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ delay: index * 0.05 }}
        >
          <StandingRow standing={standing} />
        </motion.div>
      ))}
    </div>
  );
}

function StandingRow({ standing }: { standing: StandingDto }) {
  const rankIcon = getRankIcon(standing.rank);

  return (
    <Card className={cn(
      "p-4 flex items-center gap-4 transition-colors",
      standing.rank === 1 && "bg-yellow-500/10 border-yellow-500/50",
      standing.rank === 2 && "bg-gray-400/10 border-gray-400/50",
      standing.rank === 3 && "bg-amber-600/10 border-amber-600/50",
    )}>
      {/* Rank */}
      <div className="w-10 flex justify-center">
        {rankIcon || (
          <span className="text-lg font-bold text-muted-foreground">
            {standing.rank}
          </span>
        )}
      </div>

      {/* User Info */}
      <div className="flex-1">
        <div className="font-medium">{standing.username}</div>
        <div className="text-xs text-muted-foreground">
          {standing.totalBets} bets · {standing.perfectScores} perfect
        </div>
      </div>

      {/* Points */}
      <div className="text-right">
        <div className="text-2xl font-bold">{standing.totalPoints}</div>
        <div className="text-xs text-muted-foreground">points</div>
      </div>
    </Card>
  );
}

function getRankIcon(rank: number) {
  switch (rank) {
    case 1:
      return <Trophy className="h-6 w-6 text-yellow-500" />;
    case 2:
      return <Medal className="h-6 w-6 text-gray-400" />;
    case 3:
      return <Award className="h-6 w-6 text-amber-600" />;
    default:
      return null;
  }
}
```

**Senior Dev Analysis - Framer Motion:**

| Aspect | Analysis |
|--------|----------|
| **Why?** | Preference (but high-impact for portfolio). Smooth animations make the app feel polished |
| **.NET Equivalent** | Like CSS transitions in Blazor, but with a declarative React API. `initial → animate` is like keyframe animation |
| **Backend Trap** | Don't animate on every re-render. The `key` prop ensures motion only runs when items enter/leave. Without unique keys, you get janky re-animations |

---

**Step 6.4.2: User Stats Card**

```tsx
// web/src/components/stats/user-stats-card.tsx

'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { Flame, Target, TrendingUp, Percent } from 'lucide-react';
import type { UserStatsDto } from '@/types';

interface UserStatsCardProps {
  stats: UserStatsDto;
}

export function UserStatsCard({ stats }: UserStatsCardProps) {
  const accuracyPercent = stats.totalBets > 0
    ? Math.round((stats.correctOutcomes / stats.totalBets) * 100)
    : 0;

  return (
    <Card>
      <CardHeader>
        <CardTitle>Your Stats</CardTitle>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Main Points */}
        <div className="text-center">
          <div className="text-4xl font-bold">{stats.totalPoints}</div>
          <div className="text-muted-foreground">Total Points</div>
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-2 gap-4">
          <StatItem
            icon={<Target className="h-4 w-4" />}
            label="Perfect Scores"
            value={stats.perfectScores}
          />
          <StatItem
            icon={<TrendingUp className="h-4 w-4" />}
            label="Correct Outcomes"
            value={stats.correctOutcomes}
          />
          <StatItem
            icon={<Flame className="h-4 w-4 text-orange-500" />}
            label="Current Streak"
            value={stats.currentStreak}
          />
          <StatItem
            icon={<Flame className="h-4 w-4 text-red-500" />}
            label="Best Streak"
            value={stats.bestStreak}
          />
        </div>

        {/* Accuracy Progress */}
        <div className="space-y-2">
          <div className="flex justify-between text-sm">
            <span>Prediction Accuracy</span>
            <span>{accuracyPercent}%</span>
          </div>
          <Progress value={accuracyPercent} />
        </div>
      </CardContent>
    </Card>
  );
}

function StatItem({ icon, label, value }: { icon: React.ReactNode; label: string; value: number }) {
  return (
    <div className="flex items-center gap-2">
      {icon}
      <div>
        <div className="font-medium">{value}</div>
        <div className="text-xs text-muted-foreground">{label}</div>
      </div>
    </div>
  );
}
```

---

**Verification Checklist for 6.4:**
- [ ] Standings show with rank icons for top 3
- [ ] Animation plays when standings load
- [ ] User stats display correctly with progress bar
- [ ] Empty state shows when no standings

---

## Phase 6.5: Gamification Features

### Goal
Add visual achievements, celebrations, and engagement features

### The .NET Mental Model
This is like adding event handlers that trigger animations - when certain conditions are met (perfect score, new achievement), fire visual feedback.

### Implementation

**Step 6.5.1: Confetti Celebration Hook**

```tsx
// web/src/hooks/use-confetti.ts

import confetti from 'canvas-confetti';

export function useConfetti() {
  const fire = (options?: confetti.Options) => {
    confetti({
      particleCount: 100,
      spread: 70,
      origin: { y: 0.6 },
      ...options,
    });
  };

  const firePerfectScore = () => {
    // Fire multiple bursts for perfect score
    const count = 200;
    const defaults = { origin: { y: 0.7 } };

    function fire(particleRatio: number, opts: confetti.Options) {
      confetti({
        ...defaults,
        ...opts,
        particleCount: Math.floor(count * particleRatio),
      });
    }

    fire(0.25, { spread: 26, startVelocity: 55 });
    fire(0.2, { spread: 60 });
    fire(0.35, { spread: 100, decay: 0.91, scalar: 0.8 });
    fire(0.1, { spread: 120, startVelocity: 25, decay: 0.92, scalar: 1.2 });
    fire(0.1, { spread: 120, startVelocity: 45 });
  };

  return { fire, firePerfectScore };
}
```

Add to package.json: `"canvas-confetti": "^1.9.0"`

---

**Step 6.5.2: Achievement Badge Component**

```tsx
// web/src/components/gamification/achievement-badge.tsx

import { motion } from 'framer-motion';
import { cn } from '@/lib/utils';
import {
  Trophy, Target, Flame, Star, Zap, Crown,
  Medal, Award, Rocket, Heart
} from 'lucide-react';

type AchievementType =
  | 'first_bet'
  | 'perfect_score'
  | 'streak_3'
  | 'streak_7'
  | 'top_3'
  | 'champion';

interface AchievementBadgeProps {
  type: AchievementType;
  earned: boolean;
  size?: 'sm' | 'md' | 'lg';
}

const achievementConfig: Record<AchievementType, { icon: React.ReactNode; label: string; color: string }> = {
  first_bet: { icon: <Star />, label: 'First Bet', color: 'text-blue-500' },
  perfect_score: { icon: <Target />, label: 'Perfect Score', color: 'text-green-500' },
  streak_3: { icon: <Flame />, label: '3 Day Streak', color: 'text-orange-500' },
  streak_7: { icon: <Zap />, label: 'Week Warrior', color: 'text-yellow-500' },
  top_3: { icon: <Medal />, label: 'Podium Finish', color: 'text-purple-500' },
  champion: { icon: <Crown />, label: 'Champion', color: 'text-amber-500' },
};

export function AchievementBadge({ type, earned, size = 'md' }: AchievementBadgeProps) {
  const config = achievementConfig[type];

  const sizeClasses = {
    sm: 'w-8 h-8',
    md: 'w-12 h-12',
    lg: 'w-16 h-16',
  };

  return (
    <motion.div
      whileHover={{ scale: 1.1 }}
      className={cn(
        'rounded-full flex items-center justify-center',
        sizeClasses[size],
        earned ? config.color : 'text-muted-foreground/30',
        earned ? 'bg-muted' : 'bg-muted/50',
      )}
      title={config.label}
    >
      <div className={cn(
        size === 'sm' && '[&>svg]:w-4 [&>svg]:h-4',
        size === 'md' && '[&>svg]:w-6 [&>svg]:h-6',
        size === 'lg' && '[&>svg]:w-8 [&>svg]:h-8',
      )}>
        {config.icon}
      </div>
    </motion.div>
  );
}
```

---

**Step 6.5.3: Points Animation Component**

```tsx
// web/src/components/gamification/points-earned.tsx

'use client';

import { motion, AnimatePresence } from 'framer-motion';
import { useEffect, useState } from 'react';

interface PointsEarnedProps {
  points: number;
  show: boolean;
}

export function PointsEarned({ points, show }: PointsEarnedProps) {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    if (show) {
      setVisible(true);
      const timer = setTimeout(() => setVisible(false), 2000);
      return () => clearTimeout(timer);
    }
  }, [show]);

  return (
    <AnimatePresence>
      {visible && (
        <motion.div
          initial={{ opacity: 0, y: 20, scale: 0.5 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          exit={{ opacity: 0, y: -20 }}
          className="absolute -top-8 left-1/2 -translate-x-1/2"
        >
          <div className="bg-green-500 text-white px-3 py-1 rounded-full font-bold text-sm shadow-lg">
            +{points} pts
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
```

**Senior Dev Analysis - AnimatePresence:**

| Aspect | Analysis |
|--------|----------|
| **Why?** | Necessity for exit animations. React removes DOM elements immediately - AnimatePresence delays removal until animation completes |
| **.NET Equivalent** | Like a `VisualStateManager` transition in WPF/MAUI that plays before element is removed |
| **Backend Trap** | Don't forget the `key` prop on animated children. Without it, Framer Motion can't track which element is exiting |

---

**Verification Checklist for 6.5:**
- [ ] Confetti fires on perfect score
- [ ] Achievement badges show earned/unearned states
- [ ] Points animation displays and auto-hides
- [ ] Hover effects work on badges

---

## Phase 6.6: Polish & Dark Mode

### Goal
Add dark/light mode toggle, loading skeletons, and final polish

### Implementation

**Step 6.6.1: Theme Toggle**

```tsx
// web/src/components/theme-toggle.tsx

'use client';

import { useTheme } from 'next-themes';
import { Button } from '@/components/ui/button';
import { Moon, Sun } from 'lucide-react';
import { useEffect, useState } from 'react';

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();
  const [mounted, setMounted] = useState(false);

  // Avoid hydration mismatch
  useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) {
    return <Button variant="ghost" size="icon" disabled />;
  }

  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
    >
      {theme === 'dark' ? (
        <Sun className="h-5 w-5" />
      ) : (
        <Moon className="h-5 w-5" />
      )}
    </Button>
  );
}
```

**Senior Dev Analysis - Hydration:**

| Aspect | Analysis |
|--------|----------|
| **Why?** | Necessity. Server doesn't know user's theme preference. If we render Sun on server but user has dark mode, React throws hydration error |
| **.NET Equivalent** | Like checking `HttpContext.User` - it's only available at runtime. On server-side prerender, you don't have client state |
| **Backend Trap** | `useEffect` runs ONLY on client. The `mounted` pattern ensures we don't render theme-dependent UI until client-side hydration completes |

---

**Step 6.6.2: Skeleton Components**

```tsx
// web/src/components/ui/skeletons.tsx

import { Skeleton } from '@/components/ui/skeleton';
import { Card, CardContent, CardHeader } from '@/components/ui/card';

export function LeagueCardSkeleton() {
  return (
    <Card>
      <CardHeader className="pb-2">
        <div className="flex justify-between">
          <Skeleton className="h-6 w-32" />
          <Skeleton className="h-5 w-16" />
        </div>
      </CardHeader>
      <CardContent>
        <Skeleton className="h-4 w-full mb-4" />
        <div className="flex gap-4">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-4 w-24" />
        </div>
      </CardContent>
    </Card>
  );
}

export function MatchCardSkeleton() {
  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          <div className="space-y-3 flex-1">
            <div className="flex items-center gap-2">
              <Skeleton className="h-6 w-6 rounded-full" />
              <Skeleton className="h-4 w-32" />
            </div>
            <div className="flex items-center gap-2">
              <Skeleton className="h-6 w-6 rounded-full" />
              <Skeleton className="h-4 w-28" />
            </div>
          </div>
          <Skeleton className="h-8 w-20" />
        </div>
      </CardContent>
    </Card>
  );
}

export function StandingRowSkeleton() {
  return (
    <Card className="p-4">
      <div className="flex items-center gap-4">
        <Skeleton className="h-6 w-6" />
        <div className="flex-1">
          <Skeleton className="h-5 w-32 mb-1" />
          <Skeleton className="h-3 w-24" />
        </div>
        <Skeleton className="h-8 w-16" />
      </div>
    </Card>
  );
}
```

---

**Verification Checklist for 6.6:**
- [ ] Theme toggle switches between light/dark
- [ ] No hydration errors in console
- [ ] Skeletons match layout of actual components
- [ ] All loading states use skeletons (not spinners)

---

# Part 3: File Structure Summary

After Phase 6, your `web/src/` should look like:

```
web/src/
├── app/
│   ├── (auth)/
│   │   ├── login/page.tsx
│   │   └── register/page.tsx
│   ├── (protected)/
│   │   ├── dashboard/page.tsx          # Updated
│   │   ├── leagues/
│   │   │   ├── page.tsx                # NEW - League list
│   │   │   └── [id]/page.tsx           # NEW - League detail
│   │   ├── matches/page.tsx            # NEW - All matches
│   │   └── profile/page.tsx            # NEW - User profile
│   ├── layout.tsx
│   ├── page.tsx
│   └── providers.tsx
├── components/
│   ├── auth/
│   ├── gamification/
│   │   ├── achievement-badge.tsx       # NEW
│   │   └── points-earned.tsx           # NEW
│   ├── leagues/
│   │   ├── create-league-dialog.tsx    # NEW
│   │   ├── invite-code-card.tsx        # NEW
│   │   ├── league-card.tsx             # NEW
│   │   ├── league-card-skeleton.tsx    # NEW
│   │   ├── league-list.tsx             # NEW
│   │   ├── league-standings.tsx        # NEW
│   │   └── member-list.tsx             # NEW
│   ├── matches/
│   │   ├── match-card.tsx              # NEW
│   │   ├── match-card-skeleton.tsx     # NEW
│   │   └── match-list.tsx              # NEW
│   ├── stats/
│   │   └── user-stats-card.tsx         # NEW
│   ├── ui/                             # shadcn components
│   └── theme-toggle.tsx                # NEW
├── hooks/
│   ├── use-auth.ts
│   ├── use-bets.ts                     # NEW
│   ├── use-confetti.ts                 # NEW
│   ├── use-leagues.ts                  # NEW
│   └── use-matches.ts                  # NEW
├── lib/
│   ├── api-client.ts
│   └── utils.ts
├── stores/
│   └── auth-store.ts
└── types/
    └── index.ts                        # Expanded
```

---

# Part 4: Verification Strategy

## End-to-End Test Flow

1. **Start fresh** - Clear localStorage, open app
2. **Register** - Create new account → should redirect to dashboard
3. **Create league** - Click "Create League" → fill form → see new league in list
4. **View league** - Click league card → see detail page with tabs
5. **Copy invite code** - Click copy button → toast confirms
6. **View matches** - Go to matches tab → see upcoming matches
7. **Place bet** - Enter scores → click Bet → see optimistic update
8. **Check standings** - View standings tab → see yourself ranked
9. **Toggle theme** - Click moon/sun → theme changes
10. **Refresh page** - All data persists, auth still valid

## DevTools Verification

- **React Query DevTools**: All queries cached, no duplicate fetches
- **Network tab**: Auth headers present, no 401s after login
- **Console**: No hydration errors, no React warnings
- **Lighthouse**: Accessibility score > 90

---

# Part 5: Dependencies to Add

```bash
cd web
npm install canvas-confetti date-fns
npm install -D @types/canvas-confetti
```

Also ensure these shadcn components are installed:
```bash
npx shadcn@latest add dialog tabs textarea progress badge
```
