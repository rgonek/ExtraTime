# Phase 6.3: Leaderboards & Stats


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
## Phase 6.3: Leaderboards & Stats

**Goal**: Display league standings with animations
**Duration**: ~2 implementation sessions
**Learning Focus**: Number animations, polling, memoization

### 1. Type Definitions

**File**: `web/src/types/standings.ts`
```typescript
export interface LeagueStanding {
  userId: string;
  username: string;
  totalPoints: number;
  betsPlaced: number;
  exactMatches: number;
  correctResults: number;
  currentStreak: number;
  bestStreak: number;
  rank?: number; // Calculate on frontend
}

export interface UserStats extends LeagueStanding {
  averagePointsPerBet: number;
  accuracy: number; // (exactMatches + correctResults) / betsPlaced
  rankChange: number; // +/- from previous
}
```

### 2. Custom Hooks

**File**: `web/src/hooks/use-standings.ts`
```typescript
export function useStandings(leagueId: string, enablePolling = false) {
  return useQuery<LeagueStanding[]>({
    queryKey: standingsKeys.league(leagueId),
    queryFn: () => apiClient.get(`/leagues/${leagueId}/standings`),
    staleTime: 30 * 1000,
    refetchInterval: enablePolling ? 10 * 1000 : false, // Poll every 10s during matches
    select: (data) => {
      // LEARNING: Transform data with `select`
      return data
        .sort((a, b) => b.totalPoints - a.totalPoints)
        .map((user, index) => ({ ...user, rank: index + 1 }));
    },
  });
}
```

**Learning**: `select` option transforms data without affecting cache (memoized automatically).

### 3. Components

**File**: `web/src/components/standings/leaderboard.tsx`
- Table with rank, user, points, accuracy
- Medal icons for top 3 (🥇🥈🥉)
- Highlight current user row
- **Framer Motion**: AnimatePresence for rank changes
- Expandable rows for detailed stats

**File**: `web/src/components/standings/animated-number.tsx`
```typescript
'use client';

import { animate } from 'framer-motion';
import { useEffect, useRef } from 'react';

export function AnimatedNumber({ value, duration = 1 }: { value: number; duration?: number }) {
  const nodeRef = useRef<HTMLSpanElement>(null);
  const prevValue = useRef(0);

  useEffect(() => {
    const node = nodeRef.current;
    if (!node) return;

    const controls = animate(prevValue.current, value, {
      duration,
      onUpdate(latest) {
        node.textContent = Math.round(latest).toString();
      },
    });

    prevValue.current = value;
    return () => controls.stop();
  }, [value, duration]);

  return <span ref={nodeRef}>0</span>;
}
```

**File**: `web/src/components/standings/stats-card.tsx`
- Grid layout for key metrics
- Progress bars for accuracy
- Streak badge with fire emoji 🔥
- Comparison to league average (e.g., "+5 above avg")
- **Learning**: Memoize expensive calculations

```typescript
const accuracy = useMemo(
  () => stats ? ((stats.exactMatches + stats.correctResults) / stats.betsPlaced) * 100 : 0,
  [stats]
);
```

**File**: `web/src/components/standings/mini-leaderboard.tsx`
- Compact widget for dashboard
- Top 5 users only
- Current user position (if not in top 5)
- Click to view full leaderboard

### 4. Pages

**File**: `web/src/app/(protected)/leagues/[id]/standings/page.tsx`
- Full leaderboard table
- User stats panel
- Toggle for enabling live updates during matches

### 5. Phase 6.3: Learning Summary

- **Polling**: `refetchInterval` for live standings
- **Data Transformation**: `select` option for client-side calculations
- **Number Animations**: Framer Motion's `animate` function
- **Memoization**: `useMemo` for expensive calculations
- **Conditional Styling**: Highlight current user, top 3

