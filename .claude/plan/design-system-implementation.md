# ExtraTime Design System Implementation Plan

> **Design Direction:** Vibrant Sports with Light/Dark themes
> **Font:** Space Grotesk
> **Reference:** `extratime-design-system.html`

---

## Current State Analysis

### What Already Exists

| Category | Status | Details |
|----------|--------|---------|
| Tailwind CSS 4 | ✅ | Using CSS variables in `globals.css`, no config file |
| Dark Mode | ✅ | `next-themes` with class-based switching |
| Shadcn/ui Components | ✅ | 18 components installed (button, card, badge, avatar, etc.) |
| Animations | ✅ | Framer Motion + custom animated containers |
| Gamification Components | ✅ Partial | Basic versions exist, need styling updates |
| Font | ✅ | Space Grotesk configured via next/font |
| Color Palette | ✅ | Vibrant Sports palette (emerald/blue/amber) |
| Design Tokens | ✅ | Centralized in globals.css with CSS variables |

### Key Files to Modify

```
web/src/app/globals.css          → Update CSS variables (colors, radius, shadows)
web/src/app/layout.tsx           → Change font from Inter to Space Grotesk
web/src/components/ui/*.tsx      → Update shadcn components styling
web/src/components/gamification/ → Enhance gamification components
web/src/components/leagues/      → Update league components
web/src/components/bets/         → Update betting components
web/src/components/standings/    → Update leaderboard components
```

---

## Implementation Phases

### Phase 1: Foundation - Design Tokens & Theme ✅ COMPLETED
**Goal:** Establish the core design system foundation
**Status:** Completed on 2026-01-30

#### 1.1 Update Color Palette in globals.css
**File:** `web/src/app/globals.css`

Replace current OKLCh colors with Vibrant Sports palette:

```css
/* Light Mode */
--background: #ffffff;
--foreground: #0f172a;
--card: #f8fafc;
--card-foreground: #0f172a;
--popover: #ffffff;
--popover-foreground: #0f172a;
--primary: #10b981;           /* Emerald green */
--primary-foreground: #ffffff;
--secondary: #3b82f6;         /* Blue */
--secondary-foreground: #ffffff;
--accent: #f59e0b;            /* Amber */
--accent-foreground: #ffffff;
--muted: #f8fafc;
--muted-foreground: #64748b;
--destructive: #ef4444;
--destructive-foreground: #ffffff;
--success: #22c55e;
--warning: #eab308;
--border: #e2e8f0;
--input: #e2e8f0;
--ring: #10b981;

/* Dark Mode */
--background: #0f172a;
--foreground: #f1f5f9;
--card: #1e293b;
--card-foreground: #f1f5f9;
/* ... rest of dark mode colors */
```

**Tasks:**
- [x] Backup current globals.css
- [x] Update `:root` CSS variables for light mode
- [x] Update `.dark` CSS variables for dark mode
- [x] Add new semantic colors (--success, --warning, --info)
- [x] Add component-specific colors (--primary-light, --secondary-light, etc.)

#### 1.2 Update Border Radius Tokens
**Current:** `--radius: 0.625rem` (10px)
**New:** Card: 16px, Button: 10px, Badge: 20px

```css
--radius: 0.625rem;        /* 10px - base/button */
--radius-sm: 0.375rem;     /* 6px */
--radius-md: 0.625rem;     /* 10px */
--radius-lg: 1rem;         /* 16px - cards */
--radius-xl: 1.25rem;      /* 20px - badges */
--radius-2xl: 1.5rem;      /* 24px */
--radius-full: 9999px;     /* Circles */
```

**Tasks:**
- [x] Update radius CSS variables
- [ ] Ensure components use correct radius tokens (Phase 2)

#### 1.3 Update Shadow Tokens
**Current:** Uses `shadow-xs` default
**New:** Medium shadow with hover states

```css
--shadow-sm: 0 1px 3px rgba(0,0,0,0.08);
--shadow: 0 4px 12px rgba(0,0,0,0.08);
--shadow-md: 0 4px 12px rgba(0,0,0,0.1);
--shadow-lg: 0 8px 24px rgba(0,0,0,0.12);
--shadow-glow: 0 0 20px;
```

**Tasks:**
- [x] Add shadow CSS variables
- [x] Update Tailwind theme inline to use custom shadows

#### 1.4 Configure Space Grotesk Font
**File:** `web/src/app/layout.tsx`

```tsx
import { Space_Grotesk } from 'next/font/google';

const spaceGrotesk = Space_Grotesk({
  subsets: ['latin'],
  variable: '--font-space-grotesk',
  weight: ['400', '500', '600', '700']
});
```

**Tasks:**
- [x] Install Space Grotesk via next/font
- [x] Update layout.tsx to use new font
- [x] Update globals.css `--font-sans` variable
- [x] Remove Inter font import

---

### Phase 2: Core UI Components Update
**Goal:** Update existing shadcn components to match design system

#### 2.1 Button Component
**File:** `web/src/components/ui/button.tsx`

**Changes:**
- Update border-radius to `--radius-md` (10px)
- Add colored shadows on primary/secondary/accent variants
- Add hover lift effect (`translateY(-2px)`)
- Add accent variant
- Update focus ring to use primary color

**Tasks:**
- [ ] Add `accent` variant to button variants
- [ ] Update shadow styles for each variant
- [ ] Add hover transform animation
- [ ] Update border-radius

#### 2.2 Card Component
**File:** `web/src/components/ui/card.tsx`

**Changes:**
- Update border-radius to `--radius-lg` (16px)
- Add box-shadow by default
- Add hover state with lift effect
- Add gradient card variant for headers

**Tasks:**
- [ ] Update Card base styles
- [ ] Add `variant` prop (default, gradient, elevated)
- [ ] Add hover animation styles

#### 2.3 Badge Component
**File:** `web/src/components/ui/badge.tsx`

**Changes:**
- Add semantic variants (success, warning, live, points)
- Update border-radius to `--radius-xl` (20px)
- Add live badge animation (pulse)
- Add points badge styling (+3 pts format)

**New Variants:**
```tsx
success: "bg-success/15 text-success"
warning: "bg-warning/15 text-warning"
live: "bg-destructive text-white animate-pulse"
points: "bg-primary/15 text-primary font-bold"
accent: "bg-accent/15 text-accent"
```

**Tasks:**
- [ ] Add new badge variants
- [ ] Update border-radius
- [ ] Add pulse animation for live badge

#### 2.4 Avatar Component
**File:** `web/src/components/ui/avatar.tsx`

**Changes:**
- Add size variants (xs, sm, md, lg, xl)
- Add gradient background support
- Add hover scale effect
- Add avatar stack component

**Tasks:**
- [ ] Add size variant prop
- [ ] Create AvatarStack component
- [ ] Add hover animation

#### 2.5 Progress Component
**File:** `web/src/components/ui/progress.tsx`

**Changes:**
- Add gradient fill option
- Add accent color option
- Update border-radius

**Tasks:**
- [ ] Add `variant` prop (default, gradient, accent)
- [ ] Update styling

#### 2.6 Input Component
**File:** `web/src/components/ui/input.tsx`

**Changes:**
- Update focus ring to primary color
- Add score input variant (large, centered, bold)

**Tasks:**
- [ ] Update focus styles
- [ ] Create ScoreInput variant component

---

### Phase 3: Gamification Components Enhancement
**Goal:** Enhance existing gamification components to match design system

#### 3.1 Streak Counter
**File:** `web/src/components/gamification/streak-counter.tsx`

**Enhancements:**
- Add fire icon animation (flicker effect)
- Use accent color background
- Add border with accent color
- Show day count prominently

**Tasks:**
- [ ] Update styling to match design
- [ ] Add fire animation keyframes
- [ ] Update color scheme

#### 3.2 Achievement Badge
**File:** `web/src/components/gamification/achievement-badge.tsx`

**Enhancements:**
- Add glow effect for unlocked achievements
- Add gold/silver/bronze variants
- Add locked state with grayscale
- Add hover scale/rotate animation
- Add outer ring decoration

**Tasks:**
- [ ] Add variant prop (default, gold, silver, bronze)
- [ ] Add locked state styling
- [ ] Add glow box-shadow
- [ ] Update hover animations

#### 3.3 Level Indicator
**File:** `web/src/components/gamification/level-indicator.tsx`

**Enhancements:**
- Add gradient background for level badge
- Add glow effect
- Add progress bar with gradient fill
- Show XP to next level

**Tasks:**
- [ ] Update level badge styling
- [ ] Add gradient progress bar
- [ ] Update layout

#### 3.4 Points Animation
**File:** `web/src/components/gamification/points-animation.tsx`

**Enhancements:**
- Add bounce animation on mount
- Differentiate exact score (+3) vs correct result (+1)
- Add gradient backgrounds
- Add shadow glow effect

**Tasks:**
- [ ] Update animation keyframes
- [ ] Add variant for exact vs correct
- [ ] Update color scheme

#### 3.5 Rank Badge (New Component)
**File:** `web/src/components/gamification/rank-badge.tsx`

**Create new component:**
- Gold gradient for rank 1
- Silver gradient for rank 2
- Bronze gradient for rank 3
- Neutral style for ranks 4+

**Tasks:**
- [ ] Create RankBadge component
- [ ] Add gradient backgrounds
- [ ] Export from gamification index

---

### Phase 4: Feature Components Update
**Goal:** Update league, betting, and standings components

#### 4.1 League Card
**File:** `web/src/components/leagues/league-card.tsx`

**Updates:**
- Use new Card component with shadow
- Add league icon with gradient background
- Add stats row (points, rank, streak)
- Add hover lift effect
- Update action button styling

**Tasks:**
- [ ] Update card structure
- [ ] Add icon styling
- [ ] Add stats grid
- [ ] Update button variant

#### 4.2 Match Card
**File:** `web/src/components/bets/match-card.tsx`

**Updates:**
- Add team logo styling (circular with shadow)
- Add deadline badge with accent color
- Add live match state (red badge, locked inputs)
- Add score input styling
- Add competition badge
- Add hover effect

**Tasks:**
- [ ] Update layout to match design
- [ ] Add deadline timer component
- [ ] Style score inputs
- [ ] Add state variations (upcoming, live, finished)

#### 4.3 Leaderboard Row
**File:** `web/src/components/standings/leaderboard.tsx`

**Updates:**
- Use RankBadge for positions 1-3
- Add streak indicator
- Add highlight for current user
- Update avatar styling
- Add hover state

**Tasks:**
- [ ] Integrate RankBadge component
- [ ] Update row styling
- [ ] Add current user highlight
- [ ] Update stats columns

#### 4.4 Member List
**File:** `web/src/components/leagues/member-list.tsx`

**Updates:**
- Use RankBadge component
- Add streak display
- Add hover state on rows
- Add current user highlight

**Tasks:**
- [ ] Update to use RankBadge
- [ ] Add streak indicator
- [ ] Update styling

---

### Phase 5: Layout & Navigation Update
**Goal:** Update app shell and navigation

#### 5.1 App Shell Header
**File:** `web/src/components/layout/app-shell.tsx`

**Updates:**
- Update logo styling (gradient icon)
- Update navigation button styling
- Add user menu improvements
- Update mobile menu styling

**Tasks:**
- [ ] Update logo component
- [ ] Style nav buttons with new variants
- [ ] Update user menu dropdown

#### 5.2 Page Headers
**Create consistent page header pattern:**
- Title with gradient text option
- Subtitle styling
- Action button placement

**Tasks:**
- [ ] Create PageHeader component
- [ ] Apply to existing pages

---

### Phase 6: Animation System
**Goal:** Standardize animations across the app

#### 6.1 Animation Tokens
**File:** `web/src/app/globals.css`

```css
/* Animation durations */
--duration-fast: 150ms;
--duration-normal: 200ms;
--duration-slow: 300ms;

/* Easing curves */
--ease-default: cubic-bezier(0.4, 0, 0.2, 1);
--ease-bounce: cubic-bezier(0.68, -0.55, 0.265, 1.55);
```

**Tasks:**
- [ ] Add animation CSS variables
- [ ] Add keyframe animations (bounce, pulse, fadeIn, slideUp)

#### 6.2 Update Animated Containers
**File:** `web/src/components/shared/animated-container.tsx`

**Tasks:**
- [ ] Update animation durations to use tokens
- [ ] Add new animation variants if needed

---

### Phase 7: Dark Mode Polish
**Goal:** Ensure dark mode looks great

#### 7.1 Dark Mode Color Adjustments

**Tasks:**
- [ ] Test all components in dark mode
- [ ] Adjust opacity values for backgrounds
- [ ] Ensure sufficient contrast
- [ ] Update shadow values for dark mode
- [ ] Test gamification glow effects in dark mode

#### 7.2 Dark Mode Specific Overrides

**Tasks:**
- [ ] Add dark mode specific styles where needed
- [ ] Test all pages in dark mode
- [ ] Fix any contrast issues

---

### Phase 8: Testing & Documentation
**Goal:** Ensure quality and maintainability

#### 8.1 Visual Testing

**Tasks:**
- [ ] Test all pages in light mode
- [ ] Test all pages in dark mode
- [ ] Test responsive breakpoints
- [ ] Test component interactions
- [ ] Verify animations work correctly

#### 8.2 Create Design Tokens Reference

**File:** `web/src/lib/design-tokens.ts` (optional)

Create a TypeScript file documenting all design tokens for reference.

**Tasks:**
- [ ] Document color tokens
- [ ] Document spacing tokens
- [ ] Document typography tokens
- [ ] Document animation tokens

---

## Implementation Order (Recommended)

```
Week 1: Foundation
├── Phase 1.1: Color Palette (2-3 hours)
├── Phase 1.2: Border Radius (30 min)
├── Phase 1.3: Shadows (30 min)
└── Phase 1.4: Font Setup (1 hour)

Week 1-2: Core Components
├── Phase 2.1: Button (1 hour)
├── Phase 2.2: Card (1 hour)
├── Phase 2.3: Badge (1 hour)
├── Phase 2.4: Avatar (1 hour)
├── Phase 2.5: Progress (30 min)
└── Phase 2.6: Input (1 hour)

Week 2: Gamification
├── Phase 3.1: Streak Counter (1 hour)
├── Phase 3.2: Achievement Badge (1-2 hours)
├── Phase 3.3: Level Indicator (1 hour)
├── Phase 3.4: Points Animation (1 hour)
└── Phase 3.5: Rank Badge (1 hour)

Week 2-3: Feature Components
├── Phase 4.1: League Card (1-2 hours)
├── Phase 4.2: Match Card (2-3 hours)
├── Phase 4.3: Leaderboard Row (1-2 hours)
└── Phase 4.4: Member List (1 hour)

Week 3: Layout & Polish
├── Phase 5.1: App Shell (1-2 hours)
├── Phase 5.2: Page Headers (1 hour)
├── Phase 6: Animations (1-2 hours)
└── Phase 7: Dark Mode (2-3 hours)

Week 3: Testing
└── Phase 8: Testing & Documentation (2-3 hours)
```

---

## Files Changed Summary

### Modified Files
```
web/src/app/globals.css                              # Colors, radius, shadows, animations
web/src/app/layout.tsx                               # Font change
web/src/components/ui/button.tsx                     # New variants, shadows, animations
web/src/components/ui/card.tsx                       # New variants, hover effects
web/src/components/ui/badge.tsx                      # New semantic variants
web/src/components/ui/avatar.tsx                     # Size variants, stack
web/src/components/ui/progress.tsx                   # Gradient variant
web/src/components/ui/input.tsx                      # Score input variant
web/src/components/gamification/streak-counter.tsx  # Enhanced styling
web/src/components/gamification/achievement-badge.tsx # Glow, variants
web/src/components/gamification/level-indicator.tsx # Gradient, glow
web/src/components/gamification/points-animation.tsx # Enhanced animation
web/src/components/leagues/league-card.tsx          # New design
web/src/components/bets/match-card.tsx              # New design
web/src/components/standings/leaderboard.tsx        # New design
web/src/components/leagues/member-list.tsx          # New design
web/src/components/layout/app-shell.tsx             # Updated styling
web/src/components/shared/animated-container.tsx    # Updated tokens
```

### New Files
```
web/src/components/gamification/rank-badge.tsx      # New component
web/src/components/shared/page-header.tsx           # New component
web/src/components/ui/score-input.tsx               # New component (optional)
web/src/lib/design-tokens.ts                        # Documentation (optional)
```

---

## Success Criteria

1. ✅ All colors match Vibrant Sports palette
2. ✅ Space Grotesk font applied throughout
3. ✅ Card radius is 16px, button radius is 10px
4. ✅ Shadows match design spec
5. ✅ All gamification components have glow/animation effects
6. ✅ Rank badges show gold/silver/bronze for top 3
7. ✅ Dark mode works correctly with adjusted colors
8. ✅ All interactive elements have hover states
9. ✅ Animations are smooth and consistent
10. ✅ No visual regressions in existing functionality

---

## Dependencies

No new npm packages required. Using existing:
- `tailwindcss` v4
- `framer-motion` v12
- `next/font` (built-in)
- `class-variance-authority`
- `lucide-react`

---

## Reference Files

- Design mockup: `D:\Dev\ExtraTime\extratime-design-system.html`
- Design explorer: `D:\Dev\ExtraTime\extratime-design-explorer.html`
- MVP plan: `D:\Dev\ExtraTime\.claude\plan\mvp-plan.md`
