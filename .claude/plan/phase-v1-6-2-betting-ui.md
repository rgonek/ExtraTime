# Phase 6.2: Betting System UI


This phase builds a polished, gamified betting experience on top of the completed backend APIs. The plan is structured into 4 feature-based subphases, each building upon the previous one. **Strong emphasis on learning modern React/Next.js patterns.**

### Target Audience
- **Junior Frontend Developer** - Detailed explanations of why we choose specific patterns
- **Senior Backend Developer** - Understands architecture, needs frontend-specific knowledge

### Learning Goals
1. **React Query**: Advanced patterns (optimistic updates, dependent queries, cache invalidation strategies)
2. **Framer Motion**: Animations (page transitions, celebrations, micro-interactions)
3. **React Hook Form + Zod**: Type-safe form validation
4. **Component Composition**: Compound components, render props, custom hooks
5. **Performance**: Code splitting, lazy loading, memoization, virtualization
6. **Next.js 15**: App Router, server/client components, search params, dynamic routes

### Tech Stack Recap
- **Framework**: Next.js 15 (App Router), React 19, TypeScript
- **State Management**: TanStack Query v5 (server state), Zustand (client state)
- **Styling**: Tailwind CSS 4, shadcn/ui (new-york style)
- **Animations**: Framer Motion 12
- **Forms**: React Hook Form + Zod
- **API Client**: Custom client at `src/lib/api-client.ts` with JWT refresh

### Architecture Philosophy

**Why React Query?**
- Server data should live on the server (not in local state)
- Automatic background refetching keeps data fresh
- Built-in caching reduces API calls
- Optimistic updates for instant UX
- Alternative: SWR (simpler but less powerful), Redux (overkill for server state)

**Why React Hook Form + Zod?**
- **Type Safety**: Zod schemas auto-generate TypeScript types
- **Performance**: Uncontrolled inputs = minimal re-renders
- **DX**: Single source of truth for validation
- Alternative: Formik (more re-renders), plain useState (manual validation)

**Why Framer Motion?**
- Declarative animation syntax
- Layout animations (automatic positioning)
- 60fps GPU-accelerated
- Alternative: CSS animations (less control), GSAP (imperative, larger bundle)

---
## Phase 6.2: Betting System UI

**Goal**: Users can view matches, place bets, and see results
**Duration**: ~3-4 implementation sessions
**Learning Focus**: Dependent queries, optimistic bet updates, date handling, animations

### 1. Install Additional Components

```bash
npx shadcn@latest add calendar
npx shadcn@latest add popover
npx shadcn@latest add scroll-area
npx shadcn@latest add checkbox
```

### 2. Type Definitions

**File**: `web/src/types/matches.ts`

```typescript
export enum MatchStatus {
  SCHEDULED = 'SCHEDULED',
  TIMED = 'TIMED',
  IN_PLAY = 'IN_PLAY',
  PAUSED = 'PAUSED',
  FINISHED = 'FINISHED',
  POSTPONED = 'POSTPONED',
  CANCELLED = 'CANCELLED',
  SUSPENDED = 'SUSPENDED',
}

export interface Team {
  id: string;
  externalId: number;
  name: string;
  shortName: string;
  tla: string; // Three-letter abbreviation
  logoUrl: string | null;
}

export interface Competition {
  id: string;
  externalId: number;
  name: string;
  code: string;
  country: string;
  logoUrl: string | null;
}

export interface Match {
  id: string;
  externalId: number;
  competitionId: string;
  competition: Competition;
  homeTeamId: string;
  homeTeam: Team;
  awayTeamId: string;
  awayTeam: Team;
  matchDateUtc: string;
  status: MatchStatus;
  matchday: number | null;
  stage: string | null;
  group: string | null;
  homeScore: number | null;
  awayScore: number | null;
  homeHalfTimeScore: number | null;
  awayHalfTimeScore: number | null;
  venue: string | null;
}
```

**File**: `web/src/types/bets.ts`

```typescript
export interface Bet {
  id: string;
  leagueId: string;
  userId: string;
  matchId: string;
  predictedHomeScore: number;
  predictedAwayScore: number;
  placedAt: string;
  lastUpdatedAt: string;
}

export interface BetWithMatch extends Bet {
  match: Match;
}

export interface BetResult {
  betId: string;
  pointsEarned: number;
  isExactMatch: boolean;
  isCorrectResult: boolean;
  calculatedAt: string;
}

export interface MatchBetDto {
  userId: string;
  username: string;
  predictedHomeScore: number;
  predictedAwayScore: number;
  pointsEarned: number | null;
  placedAt: string;
}

export interface PlaceBetRequest {
  matchId: string;
  predictedHomeScore: number;
  predictedAwayScore: number;
}
```

### 3. Custom Hooks & Remaining Implementation

**Note**: Due to the comprehensive detail in Phase 6.1, Phases 6.2-6.4 are summarized with key concepts. Full component code can be developed during implementation using the established patterns from Phase 6.1.

#### Matches & Bets Hooks (`web/src/hooks/use-matches.ts`, `use-bets.ts`)

**Key Patterns**:
- Query keys hierarchy for matches (all, lists, details, competitions)
- Filtering with URL search params
- **Optimistic bet placement** (onMutate, rollback on error)
- Polling for live match updates (`refetchInterval`)
- Dependent queries (fetch match only if matchId exists)

**Learning: Optimistic Update Pattern**
```typescript
// 1. Cancel outgoing queries
await queryClient.cancelQueries({ queryKey: betKeys.myBets(leagueId) });

// 2. Snapshot current state
const previous = queryClient.getQueryData(betKeys.myBets(leagueId));

// 3. Optimistically update UI
queryClient.setQueryData(betKeys.myBets(leagueId), (old) => updateFn(old));

// 4. Return context for rollback
return { previous };

// 5. onError: Rollback
queryClient.setQueryData(betKeys.myBets(leagueId), context.previous);

// 6. onSuccess: Refetch to sync
queryClient.invalidateQueries({ queryKey: betKeys.myBets(leagueId) });
```

### 4. Phase 6.2: Betting System UI - Components

**File**: `web/src/components/bets/match-card.tsx`
- Display team logos, names, date/time
- Show user's current bet if exists
- Betting deadline countdown (using `date-fns`)
- Status badge (Upcoming, Live, Finished)
- Click handler to open bet dialog
- **Framer Motion**: Card hover animation

**File**: `web/src/components/bets/bet-form-dialog.tsx`
- Dialog with match details header
- Number inputs for predicted scores (0-99 validation)
- Submit with optimistic update
- Deadline check before submission
- **Learning**: Controlled number inputs with Zod coercion

**File**: `web/src/components/bets/match-list.tsx`
- Group matches by date (Today, Tomorrow, Later)
- Filter dropdown for competitions
- Virtual scrolling with `react-window` for performance
- Empty state when no matches
- Loading skeletons

**File**: `web/src/components/bets/my-bets-view.tsx`
- Tabs: Upcoming / Live / Past
- Show predicted vs actual scores
- Points earned badge with color coding
- Edit button (only before deadline)
- Delete button with confirmation

**File**: `web/src/components/bets/bet-results-reveal.tsx`
- List all users' bets for a match (visible after deadline)
- Highlight correct predictions
- Sort by points earned
- **Framer Motion**: Staggered reveal animation

**File**: `web/src/components/bets/points-celebration.tsx`
- Confetti animation on exact match
- Animated number counter for points
- Achievement unlock modal
- **Framer Motion**: Orchestrated sequence

### 5. Phase 6.2: Pages

**File**: `web/src/app/(protected)/bets/page.tsx`
- Match list with filters (competition, date range)
- Quick bet widgets for upcoming matches
- Recent bets section
- Integration with league selector

**File**: `web/src/app/(protected)/bets/my-bets/page.tsx`
- My bets view component
- Statistics summary card
- Filter by league

### 6. Phase 6.2: Utility Functions

**File**: `web/src/lib/date-utils.ts`
```typescript
import { format, formatDistanceToNow, isPast, isFuture, addMinutes } from 'date-fns';

export function formatMatchDate(date: string): string {
  return format(new Date(date), 'EEE, MMM d @ HH:mm');
}

export function isBettingOpen(matchDate: string, deadlineMinutes: number): boolean {
  const deadline = addMinutes(new Date(matchDate), -deadlineMinutes);
  return isFuture(deadline);
}

export function getBettingDeadline(matchDate: string, deadlineMinutes: number): Date {
  return addMinutes(new Date(matchDate), -deadlineMinutes);
}

export function getTimeUntilDeadline(matchDate: string, deadlineMinutes: number): string {
  const deadline = getBettingDeadline(matchDate, deadlineMinutes);
  return formatDistanceToNow(deadline, { addSuffix: true });
}
```

### 7. Phase 6.2: Learning Summary

- **Optimistic Updates**: Instant bet placement with rollback
- **Date Handling**: Consistent date formatting and deadline logic
- **Grouped Lists**: Organize matches by date for better UX
- **Virtual Scrolling**: Performance for long match lists (react-window)
- **Celebration Animations**: Framer Motion for confetti and counters
- **Conditional UI**: Show/hide based on deadline and match status

