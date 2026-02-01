# Phase 6.6: UX Polish & Dark Mode

> **Goal:** Add loading states, dark mode, micro-animations, and accessibility
> **Backend Analogy:** Cross-cutting concerns, middleware, and configuration
> **Estimated Time:** 4-6 hours
> **Prerequisites:** Phase 6.5 complete (gamification working)

---

## What You'll Learn

| Frontend Concept | Backend Analogy | Example |
|------------------|-----------------|---------|
| CSS variables | appsettings.json | `--background: 0 0% 100%` |
| Theme context | Scoped configuration | `useTheme()` hook |
| Skeleton loading | Placeholder responses | Shimmer effect |
| Focus management | Request context | Keyboard navigation |
| Responsive design | Multi-tenant config | Mobile-first CSS |

---

## Understanding CSS Variables & Theming

### The Problem Without CSS Variables

```css
/* Without CSS variables - must duplicate everything */
.button-light {
  background: white;
  color: black;
}

.button-dark {
  background: black;
  color: white;
}

/* Component needs to know current theme */
<button className={isDark ? 'button-dark' : 'button-light'}>
```

### With CSS Variables (The Solution)

```css
/* Define variables once */
:root {
  --background: white;
  --foreground: black;
}

.dark {
  --background: black;
  --foreground: white;
}

/* Components use variables - theme-agnostic */
.button {
  background: var(--background);
  color: var(--foreground);
}

/* Component doesn't need to know theme */
<button className="button">
```

### Backend Analogy: Configuration Providers

```csharp
// Backend: Configuration that changes per environment
public class AppSettings
{
    public string BackgroundColor { get; set; }
    public string ForegroundColor { get; set; }
}

// appsettings.json (production)
{ "BackgroundColor": "white", "ForegroundColor": "black" }

// appsettings.Development.json
{ "BackgroundColor": "black", "ForegroundColor": "white" }

// Services use IOptions<AppSettings>, unaware of which config loaded
public class ButtonService(IOptions<AppSettings> settings)
{
    public string GetBackground() => settings.Value.BackgroundColor;
}

// Frontend CSS variables work the same way:
// Components use var(--background), unaware of current theme
```

---

## Step 1: Configure Theme Provider

### File: `web/src/app/providers.tsx` (Update)

```typescript
'use client';

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { ThemeProvider } from 'next-themes';
import { Toaster } from '@/components/ui/sonner';
import { useState } from 'react';

export function Providers({ children }: { children: React.ReactNode }) {
  // Create QueryClient inside component to avoid sharing between requests
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60 * 1000, // 1 minute
            retry: 1,
          },
        },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider
        attribute="class"
        defaultTheme="system"
        enableSystem
        disableTransitionOnChange={false}
      >
        {children}
        <Toaster richColors position="top-right" />
      </ThemeProvider>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
```

**Decision Analysis: Theme Implementation**

| Approach | Why Use | Why Not |
|----------|---------|---------|
| **next-themes (chosen)** | SSR-safe, no flash, system preference | Extra dependency |
| CSS media query only | Simple, no JS | Can't persist user choice |
| Manual context | Full control | SSR hydration issues |

---

## Step 2: Create Theme Toggle Component

### File: `web/src/components/ui/theme-toggle.tsx`

```typescript
'use client';

import { useTheme } from 'next-themes';
import { Moon, Sun, Monitor } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useEffect, useState } from 'react';

/**
 * Theme toggle dropdown with system option
 *
 * Demonstrates:
 * - useTheme hook from next-themes
 * - Handling hydration (mounted state)
 * - System preference detection
 */
export function ThemeToggle() {
  const { theme, setTheme, resolvedTheme } = useTheme();
  const [mounted, setMounted] = useState(false);

  // Avoid hydration mismatch
  // On server, we don't know the theme, so render placeholder
  useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) {
    // Return placeholder with same dimensions to prevent layout shift
    return (
      <Button variant="ghost" size="icon" disabled>
        <Sun className="h-5 w-5" />
      </Button>
    );
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon">
          {resolvedTheme === 'dark' ? (
            <Moon className="h-5 w-5" />
          ) : (
            <Sun className="h-5 w-5" />
          )}
          <span className="sr-only">Toggle theme</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => setTheme('light')}>
          <Sun className="h-4 w-4 mr-2" />
          Light
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setTheme('dark')}>
          <Moon className="h-4 w-4 mr-2" />
          Dark
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setTheme('system')}>
          <Monitor className="h-4 w-4 mr-2" />
          System
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/**
 * Simple toggle button (no dropdown)
 */
export function ThemeToggleSimple() {
  const { resolvedTheme, setTheme } = useTheme();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) {
    return null;
  }

  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={() => setTheme(resolvedTheme === 'dark' ? 'light' : 'dark')}
    >
      {resolvedTheme === 'dark' ? (
        <Sun className="h-5 w-5" />
      ) : (
        <Moon className="h-5 w-5" />
      )}
      <span className="sr-only">Toggle theme</span>
    </Button>
  );
}
```

---

## Step 3: Create Enhanced Loading Skeletons

### File: `web/src/components/shared/loading-skeleton.tsx` (Update)

```typescript
import { Skeleton } from '@/components/ui/skeleton';

/**
 * Page-level skeleton with header
 */
export function PageSkeleton() {
  return (
    <div className="space-y-6 animate-in fade-in duration-500">
      {/* Header skeleton */}
      <div className="flex items-center justify-between">
        <div className="space-y-2">
          <Skeleton className="h-8 w-48" />
          <Skeleton className="h-4 w-32" />
        </div>
        <Skeleton className="h-10 w-24" />
      </div>

      {/* Content skeleton */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <CardSkeleton key={i} />
        ))}
      </div>
    </div>
  );
}

/**
 * Card skeleton with shimmer effect
 */
export function CardSkeleton() {
  return (
    <div className="rounded-lg border bg-card p-4 space-y-3">
      <div className="flex items-center gap-3">
        <Skeleton className="h-10 w-10 rounded-full" />
        <div className="space-y-2 flex-1">
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-3 w-1/2" />
        </div>
      </div>
      <Skeleton className="h-4 w-full" />
      <Skeleton className="h-4 w-2/3" />
    </div>
  );
}

/**
 * List skeleton
 */
export function CardListSkeleton({ count = 3 }: { count?: number }) {
  return (
    <div className="space-y-4 animate-in fade-in duration-500">
      {Array.from({ length: count }).map((_, i) => (
        <CardSkeleton key={i} />
      ))}
    </div>
  );
}

/**
 * Grid skeleton
 */
export function CardGridSkeleton({ count = 6 }: { count?: number }) {
  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3 animate-in fade-in duration-500">
      {Array.from({ length: count }).map((_, i) => (
        <CardSkeleton key={i} />
      ))}
    </div>
  );
}

/**
 * Table skeleton
 */
export function TableSkeleton({ rows = 5 }: { rows?: number }) {
  return (
    <div className="rounded-lg border animate-in fade-in duration-500">
      {/* Header */}
      <div className="border-b p-4 flex gap-4">
        <Skeleton className="h-4 w-16" />
        <Skeleton className="h-4 w-32 flex-1" />
        <Skeleton className="h-4 w-20" />
        <Skeleton className="h-4 w-20" />
      </div>

      {/* Rows */}
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="border-b last:border-0 p-4 flex gap-4 items-center">
          <Skeleton className="h-8 w-8 rounded-full" />
          <Skeleton className="h-4 w-32 flex-1" />
          <Skeleton className="h-4 w-16" />
          <Skeleton className="h-4 w-16" />
        </div>
      ))}
    </div>
  );
}

/**
 * Match card skeleton
 */
export function MatchCardSkeleton() {
  return (
    <div className="rounded-lg border bg-card p-4">
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
    </div>
  );
}

/**
 * Stats card skeleton
 */
export function StatsCardSkeleton() {
  return (
    <div className="rounded-lg border bg-card p-4">
      <div className="flex items-center gap-2 mb-2">
        <Skeleton className="h-4 w-4" />
        <Skeleton className="h-4 w-20" />
      </div>
      <Skeleton className="h-8 w-16" />
    </div>
  );
}
```

---

## Step 4: Create App Shell with Navigation

### File: `web/src/components/layout/app-shell.tsx`

```typescript
'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  Trophy,
  Target,
  LayoutDashboard,
  Settings,
  Menu,
  X,
} from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { ThemeToggle } from '@/components/ui/theme-toggle';
import { useAuthStore } from '@/stores/auth-store';
import { useLogout } from '@/hooks/use-auth';
import { cn } from '@/lib/utils';

interface AppShellProps {
  children: React.ReactNode;
}

const navItems = [
  { href: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/leagues', label: 'Leagues', icon: Trophy },
];

/**
 * Application shell with responsive navigation
 *
 * Demonstrates:
 * - Responsive sidebar/header
 * - Active link highlighting
 * - Mobile menu toggle
 */
export function AppShell({ children }: AppShellProps) {
  const pathname = usePathname();
  const user = useAuthStore((state) => state.user);
  const logout = useLogout();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  return (
    <div className="min-h-screen bg-gradient-to-br from-background to-muted">
      {/* Header */}
      <header className="sticky top-0 z-50 border-b bg-background/80 backdrop-blur-sm">
        <div className="container mx-auto px-4 h-14 flex items-center justify-between">
          {/* Logo */}
          <Link href="/dashboard" className="flex items-center gap-2">
            <Trophy className="h-6 w-6 text-primary" />
            <span className="font-bold text-xl">ExtraTime</span>
          </Link>

          {/* Desktop nav */}
          <nav className="hidden md:flex items-center gap-1">
            {navItems.map((item) => {
              const Icon = item.icon;
              const isActive = pathname.startsWith(item.href);
              return (
                <Link key={item.href} href={item.href}>
                  <Button
                    variant={isActive ? 'secondary' : 'ghost'}
                    size="sm"
                    className="gap-2"
                  >
                    <Icon className="h-4 w-4" />
                    {item.label}
                  </Button>
                </Link>
              );
            })}
          </nav>

          {/* Right section */}
          <div className="flex items-center gap-2">
            <ThemeToggle />

            {/* User menu (desktop) */}
            <div className="hidden md:flex items-center gap-2">
              <span className="text-sm text-muted-foreground">
                {user?.username}
              </span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => logout.mutate()}
                disabled={logout.isPending}
              >
                Sign out
              </Button>
            </div>

            {/* Mobile menu button */}
            <Button
              variant="ghost"
              size="icon"
              className="md:hidden"
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            >
              {mobileMenuOpen ? (
                <X className="h-5 w-5" />
              ) : (
                <Menu className="h-5 w-5" />
              )}
            </Button>
          </div>
        </div>

        {/* Mobile nav */}
        {mobileMenuOpen && (
          <nav className="md:hidden border-t p-4 space-y-2 bg-background">
            {navItems.map((item) => {
              const Icon = item.icon;
              const isActive = pathname.startsWith(item.href);
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  onClick={() => setMobileMenuOpen(false)}
                >
                  <Button
                    variant={isActive ? 'secondary' : 'ghost'}
                    className="w-full justify-start gap-2"
                  >
                    <Icon className="h-4 w-4" />
                    {item.label}
                  </Button>
                </Link>
              );
            })}
            <hr className="my-2" />
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground">
                {user?.username}
              </span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => logout.mutate()}
              >
                Sign out
              </Button>
            </div>
          </nav>
        )}
      </header>

      {/* Main content */}
      <main className="container mx-auto px-4 py-6">
        {children}
      </main>
    </div>
  );
}
```

---

## Step 5: Update Protected Layout

### File: `web/src/app/(protected)/layout.tsx`

```typescript
import { AppShell } from '@/components/layout/app-shell';
import { ProtectedRoute } from '@/components/auth/protected-route';

export default function ProtectedLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <ProtectedRoute>
      <AppShell>{children}</AppShell>
    </ProtectedRoute>
  );
}
```

Then update individual pages to remove their own ProtectedRoute wrappers and layout styling, since it's now in the layout.

---

## Step 6: Add Micro-Animations

### File: `web/src/components/shared/animated-container.tsx`

```typescript
'use client';

import { motion, AnimatePresence } from 'framer-motion';

interface AnimatedContainerProps {
  children: React.ReactNode;
  className?: string;
}

/**
 * Fade-in container for page transitions
 */
export function FadeIn({ children, className }: AnimatedContainerProps) {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.2 }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Slide-up container for cards and list items
 */
export function SlideUp({ children, className }: AnimatedContainerProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      transition={{ duration: 0.3 }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Staggered list container
 */
export function StaggeredList({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <motion.div
      initial="hidden"
      animate="visible"
      variants={{
        visible: {
          transition: { staggerChildren: 0.05 },
        },
      }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Staggered list item
 */
export function StaggeredItem({
  children,
  className,
}: AnimatedContainerProps) {
  return (
    <motion.div
      variants={{
        hidden: { opacity: 0, y: 20 },
        visible: { opacity: 1, y: 0 },
      }}
      className={className}
    >
      {children}
    </motion.div>
  );
}

/**
 * Scale on hover
 */
export function HoverScale({
  children,
  className,
  scale = 1.02,
}: AnimatedContainerProps & { scale?: number }) {
  return (
    <motion.div
      whileHover={{ scale }}
      whileTap={{ scale: 0.98 }}
      className={className}
    >
      {children}
    </motion.div>
  );
}
```

---

## Step 7: Create Error Boundary Component

### File: `web/src/components/shared/error-boundary.tsx`

```typescript
'use client';

import { Component, ErrorInfo, ReactNode } from 'react';
import { AlertTriangle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error?: Error;
}

/**
 * Error boundary to catch React errors
 *
 * Backend Analogy: This is like global exception handling middleware.
 * It catches unhandled errors in the component tree below it.
 *
 * Note: Error boundaries must be class components in React.
 */
export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Log error to monitoring service
    console.error('Error boundary caught:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <Card className="max-w-md mx-auto mt-8">
          <CardHeader>
            <div className="flex items-center gap-2 text-destructive">
              <AlertTriangle className="h-5 w-5" />
              <CardTitle>Something went wrong</CardTitle>
            </div>
            <CardDescription>
              An unexpected error occurred. Please try again.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {process.env.NODE_ENV === 'development' && this.state.error && (
              <pre className="text-xs bg-muted p-2 rounded overflow-auto max-h-32">
                {this.state.error.message}
              </pre>
            )}
          </CardContent>
          <CardFooter>
            <Button
              onClick={() => {
                this.setState({ hasError: false, error: undefined });
                window.location.reload();
              }}
              className="gap-2"
            >
              <RefreshCw className="h-4 w-4" />
              Try again
            </Button>
          </CardFooter>
        </Card>
      );
    }

    return this.props.children;
  }
}
```

---

## Step 8: Enhance Toast Notifications

### File: `web/src/lib/toast.ts`

```typescript
import { toast } from 'sonner';

/**
 * Enhanced toast helpers with consistent styling
 *
 * Backend Analogy: These are like logging helpers that
 * format messages consistently across the application.
 */

export const showToast = {
  success: (message: string, description?: string) => {
    toast.success(message, { description });
  },

  error: (message: string, description?: string) => {
    toast.error(message, { description });
  },

  warning: (message: string, description?: string) => {
    toast.warning(message, { description });
  },

  info: (message: string, description?: string) => {
    toast.info(message, { description });
  },

  // For async operations with loading state
  promise: <T>(
    promise: Promise<T>,
    messages: {
      loading: string;
      success: string | ((data: T) => string);
      error: string | ((error: Error) => string);
    }
  ) => {
    return toast.promise(promise, messages);
  },

  // Bet-specific toasts
  betPlaced: (homeScore: number, awayScore: number) => {
    toast.success('Bet placed!', {
      description: `Your prediction: ${homeScore} - ${awayScore}`,
    });
  },

  betUpdated: () => {
    toast.success('Bet updated!');
  },

  betDeleted: () => {
    toast.success('Bet deleted');
  },

  // Achievement unlocked
  achievementUnlocked: (name: string, icon: string) => {
    toast.success('Achievement Unlocked!', {
      description: `${icon} ${name}`,
      duration: 5000,
    });
  },

  // Points earned
  pointsEarned: (points: number, isExact: boolean) => {
    toast.success(`+${points} points!`, {
      description: isExact ? 'Exact match! 🎯' : 'Correct result!',
      duration: 4000,
    });
  },
};
```

---

## Step 9: Add Responsive Utilities

### File: `web/src/hooks/use-media-query.ts`

```typescript
'use client';

import { useState, useEffect } from 'react';

/**
 * Hook to detect media query matches
 *
 * Usage:
 * const isMobile = useMediaQuery('(max-width: 768px)');
 * const prefersDark = useMediaQuery('(prefers-color-scheme: dark)');
 */
export function useMediaQuery(query: string): boolean {
  const [matches, setMatches] = useState(false);

  useEffect(() => {
    const media = window.matchMedia(query);

    // Set initial value
    setMatches(media.matches);

    // Update on change
    const listener = (e: MediaQueryListEvent) => setMatches(e.matches);
    media.addEventListener('change', listener);

    return () => media.removeEventListener('change', listener);
  }, [query]);

  return matches;
}

/**
 * Convenience hooks for common breakpoints
 */
export function useIsMobile() {
  return useMediaQuery('(max-width: 768px)');
}

export function useIsTablet() {
  return useMediaQuery('(min-width: 769px) and (max-width: 1024px)');
}

export function useIsDesktop() {
  return useMediaQuery('(min-width: 1025px)');
}

export function usePrefersDarkMode() {
  return useMediaQuery('(prefers-color-scheme: dark)');
}

export function usePrefersReducedMotion() {
  return useMediaQuery('(prefers-reduced-motion: reduce)');
}
```

---

## Step 10: Add Accessibility Improvements

### File: `web/src/components/shared/visually-hidden.tsx`

```typescript
/**
 * Visually hidden text for screen readers
 *
 * Use this to provide context that is visually apparent
 * but needs to be explicitly stated for screen readers.
 *
 * Example:
 * <button>
 *   <TrashIcon />
 *   <VisuallyHidden>Delete item</VisuallyHidden>
 * </button>
 */
export function VisuallyHidden({ children }: { children: React.ReactNode }) {
  return (
    <span className="absolute w-px h-px p-0 -m-px overflow-hidden whitespace-nowrap border-0 clip-[rect(0,0,0,0)]">
      {children}
    </span>
  );
}
```

### File: `web/src/components/shared/skip-link.tsx`

```typescript
/**
 * Skip to main content link for keyboard users
 *
 * Add this at the top of your layout:
 * <SkipLink href="#main-content" />
 *
 * And add id="main-content" to your <main> element.
 */
export function SkipLink({ href = '#main-content' }: { href?: string }) {
  return (
    <a
      href={href}
      className="
        sr-only focus:not-sr-only
        focus:absolute focus:top-4 focus:left-4 focus:z-50
        focus:bg-background focus:px-4 focus:py-2
        focus:rounded-md focus:ring-2 focus:ring-primary
        focus:outline-none
      "
    >
      Skip to main content
    </a>
  );
}
```

---

## Step 11: Add Global Loading Indicator

### File: `web/src/components/shared/global-loading.tsx`

```typescript
'use client';

import { useIsFetching, useIsMutating } from '@tanstack/react-query';

/**
 * Global loading indicator that shows when any query/mutation is in progress
 *
 * Add this to your layout to show a subtle loading indicator
 * at the top of the page during API operations.
 */
export function GlobalLoadingIndicator() {
  const isFetching = useIsFetching();
  const isMutating = useIsMutating();

  const isLoading = isFetching > 0 || isMutating > 0;

  if (!isLoading) return null;

  return (
    <div className="fixed top-0 left-0 right-0 z-50 h-1 bg-primary/20">
      <div
        className="h-full bg-primary animate-pulse"
        style={{
          width: '30%',
          animation: 'loading 1s ease-in-out infinite',
        }}
      />
      <style jsx>{`
        @keyframes loading {
          0% { transform: translateX(-100%); }
          100% { transform: translateX(400%); }
        }
      `}</style>
    </div>
  );
}
```

---

## Step 12: Final Integration

### Update `app/(protected)/layout.tsx`:

```typescript
import { AppShell } from '@/components/layout/app-shell';
import { ProtectedRoute } from '@/components/auth/protected-route';
import { GlobalLoadingIndicator } from '@/components/shared/global-loading';
import { SkipLink } from '@/components/shared/skip-link';
import { ErrorBoundary } from '@/components/shared/error-boundary';

export default function ProtectedLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <ProtectedRoute>
      <SkipLink />
      <GlobalLoadingIndicator />
      <ErrorBoundary>
        <AppShell>{children}</AppShell>
      </ErrorBoundary>
    </ProtectedRoute>
  );
}
```

---

## Verification Checklist

After completing Phase 6.6:

- [ ] Dark mode toggle works (light/dark/system)
- [ ] Theme persists across page reloads
- [ ] No flash of wrong theme on load
- [ ] Skeleton loaders appear during data fetch
- [ ] App shell with navigation works
- [ ] Mobile menu opens/closes correctly
- [ ] Active navigation link is highlighted
- [ ] Animations respect prefers-reduced-motion
- [ ] Error boundary catches and displays errors
- [ ] Toast notifications work correctly
- [ ] Skip link visible on focus
- [ ] Global loading indicator shows during requests
- [ ] Responsive design works on mobile/tablet/desktop
- [ ] `npm run build` passes

---

## Key Learnings from This Phase

1. **CSS variables** - Theme-agnostic styling with runtime switching
2. **Hydration** - Server/client mismatch issues and how to handle them
3. **Error boundaries** - Catching React errors gracefully
4. **Skip links** - Keyboard navigation accessibility
5. **Reduced motion** - Respecting user preferences
6. **Global state indicators** - TanStack Query's useIsFetching/useIsMutating

---

## Accessibility Checklist

Ensure these are in place:

- [ ] All images have alt text
- [ ] Form inputs have labels
- [ ] Buttons have accessible names
- [ ] Color contrast meets WCAG 2.1 AA
- [ ] Focus indicators are visible
- [ ] Page has proper heading hierarchy
- [ ] Interactive elements are keyboard accessible
- [ ] ARIA labels where needed

---

## Performance Final Check

- [ ] No layout shift (CLS) on page load
- [ ] Images are optimized (next/image)
- [ ] Bundle size is reasonable (<500KB initial)
- [ ] Animations are smooth (60fps)
- [ ] No unnecessary re-renders (React DevTools)

---

## Phase 6 Complete!

Congratulations! You've completed the entire Phase 6 frontend implementation. You now have:

1. **Type-safe API integration** with TypeScript
2. **Complete League UI** with CRUD operations
3. **Betting system** with optimistic updates
4. **Leaderboard** with sorting and stats
5. **Gamification** with achievements and celebrations
6. **Polished UX** with dark mode and loading states

### What You've Learned

| Category | Concepts |
|----------|----------|
| **React Fundamentals** | Props, State, Effects, Hooks |
| **Data Fetching** | TanStack Query, Caching, Invalidation |
| **State Management** | Zustand, Query Keys, Optimistic Updates |
| **Styling** | Tailwind, CSS Variables, Theming |
| **Animation** | Framer Motion, Transitions |
| **TypeScript** | Interfaces, Generics, Type Safety |
| **Accessibility** | ARIA, Focus, Reduced Motion |

### Next Steps

- **Phase 7:** Bot System (AI players for leagues)
- **Phase 8:** Deployment & Production Setup
- **Phase 9:** Extended Football Data
