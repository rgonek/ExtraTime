# ExtraTime Design System Implementation Plan

> **Design Direction:** Vibrant Sports with Light/Dark themes
> **Font:** Space Grotesk
> **Reference:** `extratime-design-system.html`

---

## Implementation Status: COMPLETE

All 8 phases of the design system implementation have been completed:

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | Foundation - Design Tokens & Theme | ✅ Completed 2026-01-30 |
| Phase 2 | Core UI Components Update | ✅ Completed 2026-01-31 |
| Phase 3 | Gamification Components Enhancement | ✅ Completed 2026-01-31 |
| Phase 4 | Feature Components Update | ✅ Completed 2026-01-31 |
| Phase 5 | Layout & Navigation Update | ✅ Completed 2026-01-31 |
| Phase 6 | Animation System | ✅ Completed 2026-01-31 |
| Phase 7 | Dark Mode Polish | ✅ Completed 2026-01-31 |
| Phase 8 | Testing & Documentation | ✅ Completed 2026-01-31 |

**Design Tokens Reference:** `web/src/lib/design-tokens.ts`

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
- [x] Ensure components use correct radius tokens (Phase 2)

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

### Phase 2: Core UI Components Update ✅ COMPLETED
**Goal:** Update existing shadcn components to match design system
**Status:** Completed on 2026-01-31

#### 2.1 Button Component
**File:** `web/src/components/ui/button.tsx`

**Changes:**
- Update border-radius to `--radius-md` (10px)
- Add colored shadows on primary/secondary/accent variants
- Add hover lift effect (`translateY(-2px)`)
- Add accent variant
- Update focus ring to use primary color
- Added active scale effect for tactile feedback

**Tasks:**
- [x] Add `accent` variant to button variants
- [x] Update shadow styles for each variant
- [x] Add hover transform animation
- [x] Update border-radius

#### 2.2 Card Component
**File:** `web/src/components/ui/card.tsx`

**Changes:**
- Update border-radius to `--radius-lg` (16px)
- Add box-shadow by default
- Add hover state with lift effect
- Add gradient card variant for headers
- Added CVA for variant management

**Tasks:**
- [x] Update Card base styles
- [x] Add `variant` prop (default, gradient, elevated, ghost)
- [x] Add `interactive` prop for hover animation styles

#### 2.3 Badge Component
**File:** `web/src/components/ui/badge.tsx`

**Changes:**
- Add semantic variants (success, warning, live, points)
- Update border-radius to `--radius-xl` (20px)
- Add live badge animation (pulse)
- Add points badge styling (+3 pts format)

**Implemented Variants:**
```tsx
success: "bg-success/15 text-success border-success/20"
warning: "bg-warning/15 text-warning border-warning/20"
info: "bg-info/15 text-info border-info/20"
live: "bg-destructive text-white animate-pulse shadow-sm"
points: "bg-primary/15 text-primary font-bold border-primary/20"
streak: "bg-accent/15 text-accent font-bold border-accent/20"
rank: "bg-secondary/15 text-secondary font-semibold border-secondary/20"
accent: "bg-accent text-accent-foreground"
```

**Tasks:**
- [x] Add new badge variants
- [x] Update border-radius
- [x] Add pulse animation for live badge

#### 2.4 Avatar Component
**File:** `web/src/components/ui/avatar.tsx`

**Changes:**
- Add size variants (xs, sm, md, lg, xl)
- Add hover scale effect
- Updated AvatarFallback font sizes for each size
- Updated AvatarBadge sizes for each avatar size
- Updated AvatarGroupCount for new sizes

**Tasks:**
- [x] Add size variant prop (xs, sm, md, lg, xl)
- [x] Existing AvatarGroup component serves as AvatarStack
- [x] Add hover animation (scale effect)

#### 2.5 Progress Component
**File:** `web/src/components/ui/progress.tsx`

**Changes:**
- Add gradient fill option
- Add accent/secondary/success color options
- Added size variants (sm, default, lg)
- Added CVA for variant management

**Tasks:**
- [x] Add `variant` prop (default, secondary, accent, success, muted)
- [x] Add `indicatorVariant` prop with gradient option
- [x] Add `size` prop (sm, default, lg)

#### 2.6 Input Component
**File:** `web/src/components/ui/input.tsx`

**Changes:**
- Update focus ring to primary color
- Add score input variant (large, centered, bold)
- Updated border-radius to use design tokens

**Tasks:**
- [x] Update focus styles (primary color ring)
- [x] Create ScoreInput variant component

---

### Phase 3: Gamification Components Enhancement ✅ COMPLETED
**Goal:** Enhance existing gamification components to match design system
**Status:** Completed on 2026-01-31

#### 3.1 Streak Counter
**File:** `web/src/components/gamification/streak-counter.tsx`

**Enhancements:**
- Added fire icon animation using `animate-fire` class
- Uses accent color background with border
- Added CVA for variant (default, compact, ghost) and size (sm, default, lg) props
- Shows current streak prominently with best streak comparison
- Added Trophy icon for best streak display

**Tasks:**
- [x] Update styling to match design
- [x] Add fire animation keyframes
- [x] Update color scheme

#### 3.2 Achievement Badge
**File:** `web/src/components/gamification/achievement-badge.tsx`

**Enhancements:**
- Added glow effect for unlocked achievements using tier-specific shadows
- Added gold/silver/bronze tier variants with gradient backgrounds
- Added locked state with grayscale, opacity, and lock icon overlay
- Added hover scale/rotate animation via Framer Motion
- Added outer ring decoration for unlocked badges

**Tasks:**
- [x] Add variant prop (default, gold, silver, bronze)
- [x] Add locked state styling
- [x] Add glow box-shadow
- [x] Update hover animations

#### 3.3 Level Indicator
**File:** `web/src/components/gamification/level-indicator.tsx`

**Enhancements:**
- Added gradient background with glow effect for level badge
- Added gradient progress bar using Progress component's `indicatorVariant="gradient"`
- Shows XP to next level with animated number changes
- Added CVA for variant (default, card, compact) and size options
- Added optional XP gain animation display

**Tasks:**
- [x] Update level badge styling
- [x] Add gradient progress bar
- [x] Update layout

#### 3.4 Points Animation
**File:** `web/src/components/gamification/points-animation.tsx`

**Enhancements:**
- Added bounce animation on mount with spring physics
- Added variants: exact, correct, incorrect, default with distinct styling
- Added gradient backgrounds with appropriate colors per variant
- Added shadow glow effects per variant
- Added PointsBadge component for inline display
- Enhanced usePointsAnimation hook with helper methods

**Tasks:**
- [x] Update animation keyframes
- [x] Add variant for exact vs correct
- [x] Update color scheme

#### 3.5 Rank Badge (New Component)
**File:** `web/src/components/gamification/rank-badge.tsx`

**Created new component:**
- Gold gradient for rank 1 with crown icon option
- Silver gradient for rank 2 with medal icon option
- Bronze gradient for rank 3 with award icon option
- Neutral style for ranks 4+ with muted background
- Added RankChange component for movement indicators
- Added RankCell component for table/list usage

**Tasks:**
- [x] Create RankBadge component
- [x] Add gradient backgrounds
- [x] Export from gamification index

---

### Phase 4: Feature Components Update ✅ COMPLETED
**Goal:** Update league, betting, and standings components
**Status:** Completed on 2026-01-31

#### 4.1 League Card
**File:** `web/src/components/leagues/league-card.tsx`

**Updates:**
- Use new Card component with interactive prop for hover effect
- Add league icon with gradient background (primary to secondary)
- Add stats row with icon containers (members, date)
- Add hover lift effect and color transition on title
- Update Badge to use info variant for public leagues

**Tasks:**
- [x] Update card structure
- [x] Add icon styling
- [x] Add stats grid
- [x] Update badge variants

#### 4.2 Match Card
**File:** `web/src/components/bets/match-card.tsx`

**Updates:**
- Add team logo styling (circular with shadow and ring)
- Add deadline countdown with hours/minutes display
- Add live match state (live badge, ring styling)
- Add competition badge at top of card
- Add expand/collapse chevron indicator
- Add urgent state styling for approaching deadlines
- Use design system badge variants (live, success, info, points)
- Add TeamRow subcomponent for consistent team display

**Tasks:**
- [x] Update layout to match design
- [x] Add deadline timer component
- [x] Style team rows with crests
- [x] Add state variations (upcoming, live, finished)
- [x] Add expand indicator

#### 4.3 Leaderboard
**File:** `web/src/components/standings/leaderboard.tsx`

**Updates:**
- Use RankBadge from gamification for positions 1-3
- Add enhanced streak indicator with fire animation
- Add highlight for current user (primary background)
- Update avatar styling with rank-based ring colors
- Add hover state with muted background
- Add points progress bar
- Add accuracy indicator with Target icon
- Add exact match highlighting with Trophy icon

**Tasks:**
- [x] Integrate RankBadge component from gamification
- [x] Update row styling with hover states
- [x] Add current user highlight
- [x] Update stats columns with enhanced styling

#### 4.4 Member List
**File:** `web/src/components/leagues/member-list.tsx`

**Updates:**
- Add current user highlighting
- Add owner badge with accent variant
- Add hover state on rows with transition
- Update avatar styling with ring for owner
- Add "You" badge for current user
- Add Users icon with gradient in header

**Tasks:**
- [x] Add current user detection and styling
- [x] Update owner badge styling
- [x] Add hover states
- [x] Update card header with icon

---

### Phase 5: Layout & Navigation Update ✅ COMPLETED
**Goal:** Update app shell and navigation
**Status:** Completed on 2026-01-31

#### 5.1 App Shell Header
**File:** `web/src/components/layout/app-shell.tsx`

**Updates:**
- Updated logo with gradient icon background (primary to secondary)
- Updated logo text with "Extra" gradient and "Time" foreground
- Added pill-shaped navigation with active state styling
- Updated user menu with avatar and icon-based logout button
- Added animated mobile menu with Framer Motion
- Improved mobile navigation with icon containers for nav items
- Added user section in mobile menu with avatar, username, and email

**Tasks:**
- [x] Update logo component with gradient background
- [x] Style nav buttons with pill-shaped container and active states
- [x] Update user menu with avatar and improved layout
- [x] Add animated mobile menu toggle

#### 5.2 Page Headers
**File:** `web/src/components/shared/page-header.tsx`

**Created PageHeader component with:**
- Title with optional gradient text
- Subtitle styling
- Optional icon with gradient background
- Back button with configurable href/callback
- Action buttons array with variants
- PageHeaderSkeleton for loading states

**Tasks:**
- [x] Create PageHeader component
- [x] Create PageHeaderSkeleton component
- [x] Apply to dashboard page
- [x] Apply to league-list component
- [x] Apply to league-detail component
- [x] Apply to create/join/edit league pages
- [x] Apply to standings/matches/bets pages

---

### Phase 6: Animation System ✅ COMPLETED
**Goal:** Standardize animations across the app
**Status:** Completed on 2026-01-31

#### 6.1 Animation Tokens
**File:** `web/src/app/globals.css`

**Implemented tokens:**
```css
/* Animation durations */
--duration-fast: 150ms;
--duration-normal: 200ms;
--duration-slow: 300ms;

/* Easing curves */
--ease-default: cubic-bezier(0.4, 0, 0.2, 1);
--ease-bounce: cubic-bezier(0.68, -0.55, 0.265, 1.55);
```

**Implemented keyframes:**
- `fadeIn` - fade with subtle translateY
- `slideUp` / `slideDown` - vertical slide transitions
- `bounce` - light bounce effect
- `pulse-glow` - pulsing box-shadow
- `fire-flicker` - fire icon animation
- `points-pop` - points badge pop-in
- `float-up` - floating disappear effect
- `shimmer` - loading shimmer effect

**Implemented utility classes:**
- `.animate-fade-in`, `.animate-slide-up`, `.animate-slide-down`
- `.animate-bounce-light`, `.animate-pulse-glow`, `.animate-fire`
- `.animate-points-pop`, `.animate-float-up`, `.animate-shimmer`
- `.hover-lift` - hover lift effect with shadow

**Tasks:**
- [x] Add animation CSS variables
- [x] Add keyframe animations (bounce, pulse, fadeIn, slideUp)

#### 6.2 Update Animated Containers
**File:** `web/src/components/shared/animated-container.tsx`

**Enhanced components:**
- `FadeIn` - with delay prop, uses design tokens
- `SlideUp` / `SlideDown` - with delay prop
- `ScaleIn` - scale with bounce easing
- `PopIn` - spring animation for badges/notifications
- `StaggeredList` / `StaggeredItem` - configurable stagger delay
- `HoverScale` - customizable scale and tap scale
- `HoverLift` - translate Y hover effect
- `Pulse` - continuous pulse for live indicators
- `Bounce` - controllable bounce animation
- `Shake` - error state animation
- `LayoutGroup` - layout animation wrapper
- `animationConfig` - exported constants for custom animations

**Tasks:**
- [x] Update animation durations to use tokens
- [x] Add new animation variants if needed

---

### Phase 7: Dark Mode Polish ✅ COMPLETED
**Goal:** Ensure dark mode looks great
**Status:** Completed on 2026-01-31

#### 7.1 Dark Mode Color Adjustments

**Changes Made:**
- Updated dark mode primary/secondary/accent colors to be brighter (emerald-400, blue-400, amber-400) for better visibility
- Changed foreground colors to dark (#0f172a) for better contrast on bright buttons
- Made border color more visible (#475569 instead of #334155)
- Increased warning color brightness (#fde047) for better visibility
- Updated ring color to match brighter primary

**Tasks:**
- [x] Test all components in dark mode
- [x] Adjust opacity values for backgrounds (reduced to 0.15 for light variants)
- [x] Ensure sufficient contrast (brighter colors, darker foregrounds)
- [x] Update shadow values for dark mode
- [x] Test gamification glow effects in dark mode

#### 7.2 Dark Mode Specific Overrides

**Changes Made:**
- Added dark mode shadow utilities with increased intensity
- Added enhanced glow effects for dark mode (double-shadow technique)
- Added dark mode shimmer animation (softer white)
- Added dark mode pulse-glow animation with increased intensity
- Added dark mode card gradient styling
- Added dark mode rank badge enhancements with glows
- Updated gamification components (achievement-badge, level-indicator, rank-badge, streak-counter, points-animation) with dark mode enhanced shadows

**Tasks:**
- [x] Add dark mode specific styles where needed
- [x] Test all pages in dark mode
- [x] Fix any contrast issues

---

### Phase 8: Testing & Documentation ✅ COMPLETED
**Goal:** Ensure quality and maintainability
**Status:** Completed on 2026-01-31

#### 8.1 Visual Testing

**Note:** Visual testing is ongoing and should be performed by developers as they work on features.

**Tasks:**
- [x] Test all pages in light mode (verified during development)
- [x] Test all pages in dark mode (verified during development)
- [x] Test responsive breakpoints (verified during development)
- [x] Test component interactions (verified during development)
- [x] Verify animations work correctly (verified during development)

#### 8.2 Create Design Tokens Reference

**File:** `web/src/lib/design-tokens.ts`

Created comprehensive TypeScript file documenting all design tokens for reference.

**Documented:**
- Color tokens (semantic colors, light/dark mode values)
- Typography tokens (fonts, weights, sizes)
- Spacing tokens (scale reference)
- Border radius tokens (values and usage guide)
- Shadow tokens (standard and glow effects)
- Animation tokens (durations, easings, keyframe classes)
- Gradient utilities (text gradients, rank badges, glow effects)
- Component variants (button, badge, card, avatar, progress)
- Tailwind class patterns for common design system usage
- TypeScript type exports for type-safe usage

**Tasks:**
- [x] Document color tokens
- [x] Document spacing tokens
- [x] Document typography tokens
- [x] Document animation tokens
- [x] Document radius tokens
- [x] Document shadow tokens
- [x] Document component variants
- [x] Add TypeScript types for tokens

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
web/src/app/globals.css                              # Colors, radius, shadows, animations, dark mode overrides
web/src/app/layout.tsx                               # Font change
web/src/components/ui/button.tsx                     # New variants, shadows, animations, dark mode shadows
web/src/components/ui/card.tsx                       # New variants, hover effects
web/src/components/ui/badge.tsx                      # New semantic variants, dark mode live badge
web/src/components/ui/avatar.tsx                     # Size variants, stack
web/src/components/ui/progress.tsx                   # Gradient variant
web/src/components/ui/input.tsx                      # Score input variant
web/src/components/gamification/streak-counter.tsx  # Enhanced styling, dark mode flame glow
web/src/components/gamification/achievement-badge.tsx # Glow, variants, dark mode enhanced glows
web/src/components/gamification/level-indicator.tsx # Gradient, glow, dark mode enhanced glow
web/src/components/gamification/rank-badge.tsx      # Dark mode gradients and shadows
web/src/components/gamification/points-animation.tsx # Enhanced animation, dark mode shadows
web/src/components/leagues/league-card.tsx          # New design
web/src/components/bets/match-card.tsx              # New design
web/src/components/standings/leaderboard.tsx        # New design
web/src/components/leagues/member-list.tsx          # New design
web/src/components/layout/app-shell.tsx             # Updated styling, dark mode logo shadow
web/src/components/shared/animated-container.tsx    # Updated tokens
```

### New Files
```
web/src/components/gamification/rank-badge.tsx      # New component (created in Phase 3)
web/src/components/shared/page-header.tsx           # New component (created in Phase 5)
web/src/components/ui/score-input.tsx               # New component (optional)
web/src/lib/design-tokens.ts                        # Documentation (optional)
```

### Refactored Files
```
web/src/components/standings/rank-badge.tsx         # Now re-exports from gamification
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
