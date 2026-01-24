# Phase 6: Frontend Implementation & Polish - Overview

## Introduction

Phase 6 brings the complete backend APIs to life with a polished, gamified frontend experience. This phase is divided into 4 subphases for easier implementation.

> **Complete Educational Plan**: See [phase-6-comprehensive.md](./phase-6-comprehensive.md) for the full 2300+ line plan with detailed code examples and learning explanations.

## Subphases

| Phase | File | Duration | Description |
|-------|------|----------|-------------|
| 6.1 | [phase-6-1-league-ui.md](./phase-6-1-league-ui.md) | ~2-3 sessions | Create, join, manage leagues |
| 6.2 | [phase-6-2-betting-ui.md](./phase-6-2-betting-ui.md) | ~3-4 sessions | View matches, place bets, see results |
| 6.3 | [phase-6-3-leaderboards.md](./phase-6-3-leaderboards.md) | ~2 sessions | Display rankings with animations |
| 6.4 | [phase-6-4-polish.md](./phase-6-4-polish.md) | ~2-3 sessions | Achievements, performance, accessibility |

## Learning Goals

1. **React Query**: Advanced patterns (optimistic updates, dependent queries, cache invalidation)
2. **Framer Motion**: Animations (page transitions, celebrations, micro-interactions)
3. **React Hook Form + Zod**: Type-safe form validation
4. **Component Composition**: Compound components, render props, custom hooks
5. **Performance**: Code splitting, lazy loading, memoization, virtualization
6. **Next.js 15**: App Router, server/client components, search params, dynamic routes

## Tech Stack

- **Framework**: Next.js 15 (App Router), React 19, TypeScript
- **State**: TanStack Query v5 (server state), Zustand (client state)
- **Styling**: Tailwind CSS 4, shadcn/ui (new-york style)
- **Animations**: Framer Motion 12
- **Forms**: React Hook Form + Zod
- **API Client**: Custom client at `src/lib/api-client.ts`

## Architecture Philosophy

### Why React Query?
- Server data lives on the server (not in local state)
- Automatic background refetching
- Built-in caching reduces API calls
- Optimistic updates for instant UX

### Why React Hook Form + Zod?
- **Type Safety**: Zod schemas auto-generate TypeScript types
- **Performance**: Uncontrolled inputs = minimal re-renders
- **DX**: Single source of truth for validation

### Why Framer Motion?
- Declarative animation syntax
- Layout animations (automatic positioning)
- 60fps GPU-accelerated

## Implementation Strategy

### Week-by-Week Breakdown

**Week 1: Phase 6.1 - League System** (Foundation)
- Day 1-2: Types, hooks, create dialog
- Day 3-4: League list, detail pages
- Day 5: Join flow, polish

**Week 2: Phase 6.2 - Betting System** (Core Feature)
- Day 1-2: Match & bet hooks, optimistic updates
- Day 3-4: Match list, bet placement, my bets
- Day 5: Celebrations, testing

**Week 3: Phase 6.3 - Leaderboards** (Engagement)
- Day 1-2: Standings hooks, leaderboard table
- Day 3-5: Stats cards, animations, dashboard

**Week 4: Phase 6.4 - Polish** (Portfolio Quality)
- Day 1: Achievement system
- Day 2: Theme toggle, skeletons
- Day 3: Performance optimizations
- Day 4: Landing page, accessibility
- Day 5: Final polish, testing

## Verification Steps

After each phase:

1. **Run Development Server**: `cd web && npm run dev`
2. **Test User Flows**: Create league → Place bet → Check leaderboard
3. **Check React Query DevTools**: Verify cache updates
4. **Performance**: Run Lighthouse audit (target > 90)

## Success Criteria

After completing Phase 6:

**Technical Skills**:
- ✅ Mastery of React Query patterns
- ✅ Proficiency with Framer Motion
- ✅ Form handling (RHF + Zod)
- ✅ Performance optimization
- ✅ Accessibility best practices

**Portfolio Project**:
- ✅ Polished, production-ready app
- ✅ Gamified UX with animations
- ✅ Responsive across all devices
- ✅ Clean, maintainable codebase

## Next Steps

Start with **Phase 6.1** (League System UI) - this establishes all the patterns you'll reuse in subsequent phases.
