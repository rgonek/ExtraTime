# Phase 6: Frontend Implementation & Polish - Detailed Educational Plan

## Overview

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
## Phase 6.1: League System UI

**Goal**: Complete league creation, discovery, joining, and management
**Duration**: ~2-3 implementation sessions
**Learning Focus**: Forms, modals, list rendering, React Query patterns

### 1. Install shadcn/ui Components

```bash
# Run these from web/ directory
npx shadcn@latest add dialog
npx shadcn@latest add form
npx shadcn@latest add select
npx shadcn@latest add badge
npx shadcn@latest add separator
npx shadcn@latest add avatar
npx shadcn@latest add dropdown-menu
npx shadcn@latest add skeleton
npx shadcn@latest add tabs
npx shadcn@latest add tooltip
npx shadcn@latest add alert-dialog
npx shadcn@latest add switch
npx shadcn@latest add textarea
```

**Why shadcn/ui?**
- Copy-paste components (you own the code)
- Radix UI primitives (accessible by default)
- Tailwind styling (easy customization)
- Alternative: Material UI (heavier), Chakra (different paradigm)

### 2. Type Definitions

**File**: `web/src/types/leagues.ts`

```typescript
// Backend DTOs - match your C# response models exactly
export interface League {
  id: string;
  name: string;
  description: string | null;
  ownerId: string;
  ownerUsername: string;
  isPublic: boolean;
  maxMembers: number;
  currentMemberCount: number;
  scoreExactMatch: number;
  scoreCorrectResult: number;
  bettingDeadlineMinutes: number;
  allowedCompetitionIds: string[] | null;
  inviteCode: string;
  inviteCodeExpiresAt: string | null;
  createdAt: string;
}

export interface LeagueSummary {
  id: string;
  name: string;
  ownerUsername: string;
  memberCount: number;
  isPublic: boolean;
  createdAt: string;
}

export interface LeagueMember {
  userId: string;
  username: string;
  email: string;
  role: 'Member' | 'Owner';
  joinedAt: string;
}

export interface LeagueDetail extends League {
  members: LeagueMember[];
}

// Request types for API calls
export interface CreateLeagueRequest {
  name: string;
  description?: string;
  isPublic: boolean;
  maxMembers: number;
  scoreExactMatch: number;
  scoreCorrectResult: number;
  bettingDeadlineMinutes: number;
  allowedCompetitionIds?: string[];
  inviteCodeExpiresAt?: string;
}

export interface UpdateLeagueRequest extends Partial<CreateLeagueRequest> {}

export interface JoinLeagueRequest {
  inviteCode: string;
}
```

**Learning Point**: Keep types in sync with backend DTOs. Consider generating types from OpenAPI spec in production.

### 3. Custom Hooks (React Query Patterns)

**File**: `web/src/hooks/use-leagues.ts`

This is the most important file - it demonstrates core React Query patterns.

```typescript
'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { League, LeagueSummary, LeagueDetail, CreateLeagueRequest } from '@/types/leagues';
import { toast } from 'sonner';

// LEARNING: Query Key Factory Pattern
// Hierarchical keys enable smart cache invalidation
// Example: invalidate all leagues, or just one league's details
export const leagueKeys = {
  all: ['leagues'] as const,
  lists: () => [...leagueKeys.all, 'list'] as const,
  list: (filters?: Record<string, unknown>) => [...leagueKeys.lists(), filters] as const,
  details: () => [...leagueKeys.all, 'detail'] as const,
  detail: (id: string) => [...leagueKeys.details(), id] as const,
} as const;

// WHY: This pattern allows:
// - invalidateQueries({ queryKey: leagueKeys.all }) -> invalidates everything
// - invalidateQueries({ queryKey: leagueKeys.lists() }) -> invalidates all lists
// - invalidateQueries({ queryKey: leagueKeys.detail(id) }) -> invalidates one league

// HOOK: List user's leagues
export function useLeagues() {
  return useQuery<LeagueSummary[]>({
    queryKey: leagueKeys.lists(),
    queryFn: () => apiClient.get<LeagueSummary[]>('/leagues'),
    staleTime: 5 * 60 * 1000, // 5 minutes
    // LEARNING: staleTime vs gcTime (formerly cacheTime)
    // - staleTime: How long data is considered "fresh" (won't refetch)
    // - gcTime: How long inactive data stays in cache (default 5 min)
    // For frequently viewed data (leagues list): higher staleTime reduces API calls
  });
}

// HOOK: Get single league details
export function useLeague(leagueId: string | undefined) {
  return useQuery<LeagueDetail>({
    queryKey: leagueKeys.detail(leagueId!),
    queryFn: () => apiClient.get<LeagueDetail>(`/leagues/${leagueId}`),
    staleTime: 2 * 60 * 1000, // 2 minutes (shorter than list)
    enabled: !!leagueId, // LEARNING: Only fetch if leagueId exists
    // WHY: Prevents 404 errors during loading states or navigation
  });
}

// MUTATION: Create league
export function useCreateLeague() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateLeagueRequest) =>
      apiClient.post<League>('/leagues', data),

    // LEARNING: Optimistic Updates Pattern (3 steps)
    // 1. onMutate: Update cache before API call (optimistic)
    // 2. onError: Rollback if API fails
    // 3. onSettled: Refetch to ensure sync with server

    onSuccess: (newLeague) => {
      // Strategy 1: Invalidate (trigger refetch)
      queryClient.invalidateQueries({ queryKey: leagueKeys.lists() });

      // Strategy 2: Manual cache update (instant UI update)
      queryClient.setQueryData<LeagueSummary[]>(
        leagueKeys.lists(),
        (old) => old ? [...old, {
          id: newLeague.id,
          name: newLeague.name,
          ownerUsername: newLeague.ownerUsername,
          memberCount: 1,
          isPublic: newLeague.isPublic,
          createdAt: newLeague.createdAt,
        }] : [{ id: newLeague.id, name: newLeague.name, ownerUsername: newLeague.ownerUsername, memberCount: 1, isPublic: newLeague.isPublic, createdAt: newLeague.createdAt }]
      );

      toast.success(`League "${newLeague.name}" created! 🎉`);
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to create league');
    },
  });
}

// MUTATION: Join league
export function useJoinLeague() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ leagueId, inviteCode }: { leagueId: string; inviteCode: string }) =>
      apiClient.post(`/leagues/${leagueId}/join`, { inviteCode }),
    onSuccess: () => {
      // Invalidate all league queries (user's list changed)
      queryClient.invalidateQueries({ queryKey: leagueKeys.all });
      toast.success('Welcome to the league! 🏆');
    },
  });
}

// MUTATION: Update league (owner only)
export function useUpdateLeague(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: Partial<CreateLeagueRequest>) =>
      apiClient.put<League>(`/leagues/${leagueId}`, data),
    onSuccess: (updated) => {
      // Update detail cache directly
      queryClient.setQueryData(leagueKeys.detail(leagueId), updated);
      // Invalidate lists (name might have changed)
      queryClient.invalidateQueries({ queryKey: leagueKeys.lists() });
      toast.success('League settings updated');
    },
  });
}

// MUTATION: Delete league
export function useDeleteLeague() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (leagueId: string) => apiClient.delete(`/leagues/${leagueId}`),
    onSuccess: (_, leagueId) => {
      // Remove from cache entirely
      queryClient.removeQueries({ queryKey: leagueKeys.detail(leagueId) });
      queryClient.invalidateQueries({ queryKey: leagueKeys.lists() });
      toast.success('League deleted');
    },
  });
}

// MUTATION: Leave league
export function useLeaveLeague() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (leagueId: string) => apiClient.delete(`/leagues/${leagueId}/leave`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: leagueKeys.all });
      toast.success('Left league');
    },
  });
}

// MUTATION: Kick member (owner only)
export function useKickMember(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: string) =>
      apiClient.delete(`/leagues/${leagueId}/members/${userId}`),
    onSuccess: () => {
      // Only invalidate this league's details (member list changed)
      queryClient.invalidateQueries({ queryKey: leagueKeys.detail(leagueId) });
      toast.success('Member removed');
    },
  });
}

// MUTATION: Regenerate invite code
export function useRegenerateInviteCode(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (expiresAt?: Date) =>
      apiClient.post<{ inviteCode: string }>(
        `/leagues/${leagueId}/invite-code/regenerate`,
        expiresAt ? { expiresAt: expiresAt.toISOString() } : undefined
      ),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: leagueKeys.detail(leagueId) });
      toast.success('New invite code generated');
    },
  });
}
```

**Key Takeaways**:
1. **Query keys are hierarchical** - enables targeted cache invalidation
2. **enabled option** - prevents unnecessary API calls
3. **staleTime** - balance between freshness and performance
4. **Optimistic updates** - instant UI feedback (use for critical UX)
5. **Toast notifications** - centralized user feedback

### 4. Utility Hooks

**File**: `web/src/hooks/use-copy-to-clipboard.ts`

```typescript
'use client';

import { useState } from 'react';
import { toast } from 'sonner';

// LEARNING: Custom hooks extract reusable logic
// WHY: Used in multiple places (invite codes, share links)
export function useCopyToClipboard() {
  const [isCopied, setIsCopied] = useState(false);

  const copy = async (text: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setIsCopied(true);
      toast.success('Copied to clipboard! 📋');

      // Auto-reset after 2 seconds
      setTimeout(() => setIsCopied(false), 2000);
    } catch {
      toast.error('Failed to copy');
    }
  };

  return { isCopied, copy };
}
```

### 5. Components to Build

#### Component: Create League Dialog

**File**: `web/src/components/leagues/create-league-dialog.tsx`

Key patterns demonstrated:
- **Controlled dialog state** (local useState)
- **React Hook Form** with Zod resolver
- **Form field composition** (FormField render prop)
- **Number input handling** (string to number conversion)

```typescript
'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Textarea } from '@/components/ui/textarea';
import { Separator } from '@/components/ui/separator';
import { useCreateLeague } from '@/hooks/use-leagues';

// LEARNING: Zod Schema - Single Source of Truth
// Benefits:
// 1. Runtime validation
// 2. TypeScript types auto-generated (z.infer)
// 3. Error messages defined here
// 4. Can be shared with backend (if using Node.js)
const createLeagueSchema = z.object({
  name: z.string()
    .min(3, 'Name must be at least 3 characters')
    .max(100, 'Name too long'),
  description: z.string().max(500).optional().or(z.literal('')),
  isPublic: z.boolean().default(false),
  maxMembers: z.coerce.number() // LEARNING: coerce converts string input to number
    .int()
    .min(2, 'At least 2 members required')
    .max(255)
    .default(10),
  scoreExactMatch: z.coerce.number()
    .int()
    .min(0)
    .max(100)
    .default(3),
  scoreCorrectResult: z.coerce.number()
    .int()
    .min(0)
    .max(100)
    .default(1),
  bettingDeadlineMinutes: z.coerce.number()
    .int()
    .min(0)
    .max(120)
    .default(5),
});

type CreateLeagueFormData = z.infer<typeof createLeagueSchema>;

interface CreateLeagueDialogProps {
  children: React.ReactNode;
}

export function CreateLeagueDialog({ children }: CreateLeagueDialogProps) {
  const [open, setOpen] = useState(false);
  const createLeague = useCreateLeague();

  // LEARNING: React Hook Form Setup
  // - resolver: Connects Zod schema
  // - defaultValues: Initialize form (required for controlled behavior)
  const form = useForm<CreateLeagueFormData>({
    resolver: zodResolver(createLeagueSchema),
    defaultValues: {
      name: '',
      description: '',
      isPublic: false,
      maxMembers: 10,
      scoreExactMatch: 3,
      scoreCorrectResult: 1,
      bettingDeadlineMinutes: 5,
    },
  });

  const onSubmit = async (data: CreateLeagueFormData) => {
    await createLeague.mutateAsync(data);
    setOpen(false);
    form.reset(); // Clear form for next use
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        {/* LEARNING: asChild - renders children instead of button wrapper */}
        {children}
      </DialogTrigger>

      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Create New League</DialogTitle>
          <DialogDescription>
            Set up your betting league with custom scoring rules
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          {/* LEARNING: form.handleSubmit wraps onSubmit with validation */}
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">

            {/* Basic Information */}
            <div className="space-y-4">
              <FormField
                control={form.control}
                name="name"
                render={({ field }) => (
                  // LEARNING: Render prop pattern
                  // field = { value, onChange, onBlur, ref, name }
                  <FormItem>
                    <FormLabel>League Name</FormLabel>
                    <FormControl>
                      <Input placeholder="Premier Predictions" {...field} />
                    </FormControl>
                    <FormMessage /> {/* Auto-shows validation errors */}
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="description"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Description (Optional)</FormLabel>
                    <FormControl>
                      <Textarea
                        placeholder="A friendly competition among friends..."
                        className="resize-none"
                        rows={3}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <Separator />

            {/* League Settings */}
            <div className="space-y-4">
              <h3 className="font-medium">League Settings</h3>

              <FormField
                control={form.control}
                name="maxMembers"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Maximum Members</FormLabel>
                    <FormControl>
                      <Input type="number" {...field} />
                    </FormControl>
                    <FormDescription>
                      How many people can join (2-255)
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="isPublic"
                render={({ field }) => (
                  <FormItem className="flex items-center justify-between rounded-lg border p-4">
                    <div className="space-y-0.5">
                      <FormLabel className="text-base">Public League</FormLabel>
                      <FormDescription>
                        Anyone can find and join without invite code
                      </FormDescription>
                    </div>
                    <FormControl>
                      <Switch
                        checked={field.value}
                        onCheckedChange={field.onChange}
                      />
                    </FormControl>
                  </FormItem>
                )}
              />
            </div>

            <Separator />

            {/* Scoring Rules */}
            <div className="space-y-4">
              <h3 className="font-medium">Scoring Rules</h3>

              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="scoreExactMatch"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Exact Score</FormLabel>
                      <FormControl>
                        <Input type="number" {...field} />
                      </FormControl>
                      <FormDescription>Points awarded</FormDescription>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="scoreCorrectResult"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Correct Result</FormLabel>
                      <FormControl>
                        <Input type="number" {...field} />
                      </FormControl>
                      <FormDescription>Win/draw/loss</FormDescription>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="bettingDeadlineMinutes"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Betting Deadline</FormLabel>
                    <FormControl>
                      <Input type="number" {...field} />
                    </FormControl>
                    <FormDescription>
                      Minutes before kickoff (0-120)
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            {/* Actions */}
            <div className="flex justify-end gap-3">
              <Button
                type="button"
                variant="outline"
                onClick={() => setOpen(false)}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={createLeague.isPending}
              >
                {createLeague.isPending ? 'Creating...' : 'Create League'}
              </Button>
            </div>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}
```

**Learning Summary**:
- ✅ Controlled dialog state with useState
- ✅ React Hook Form with Zod validation
- ✅ FormField render prop pattern
- ✅ z.coerce for number inputs
- ✅ Async mutation handling
- ✅ Form reset after success

#### Component: League Card

**File**: `web/src/components/leagues/league-card.tsx`

Demonstrates: Framer Motion animations, card hover effects

```typescript
'use client';

import { motion } from 'framer-motion';
import { Users, Crown, Calendar } from 'lucide-react';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import type { LeagueSummary } from '@/types/leagues';
import { formatDistanceToNow } from 'date-fns';

interface LeagueCardProps {
  league: LeagueSummary;
  onClick?: () => void;
}

export function LeagueCard({ league, onClick }: LeagueCardProps) {
  return (
    // LEARNING: Framer Motion Basics
    // - motion.div: Animated version of div
    // - layout: Automatically animates position changes (list reordering)
    // - whileHover: Animation during hover state
    // - initial/animate: Enter animations
    <motion.div
      layout
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, scale: 0.95 }}
      whileHover={{ y: -4, transition: { duration: 0.2 } }}
      whileTap={{ scale: 0.98 }} // Subtle press effect
    >
      <Card
        className="cursor-pointer hover:shadow-lg transition-shadow"
        onClick={onClick}
      >
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <div className="space-y-1">
            <h3 className="font-semibold text-lg">{league.name}</h3>
            <div className="flex items-center gap-1 text-sm text-muted-foreground">
              <Crown className="size-3" />
              <span>{league.ownerUsername}</span>
            </div>
          </div>
          {league.isPublic && (
            <Badge variant="secondary">Public</Badge>
          )}
        </CardHeader>

        <CardContent>
          <div className="flex items-center gap-4 text-sm text-muted-foreground">
            <div className="flex items-center gap-1.5">
              <Users className="size-4" />
              <span>{league.memberCount} members</span>
            </div>
            <div className="flex items-center gap-1.5">
              <Calendar className="size-4" />
              <span>
                {formatDistanceToNow(new Date(league.createdAt), {
                  addSuffix: true
                })}
              </span>
            </div>
          </div>
        </CardContent>
      </Card>
    </motion.div>
  );
}
```

**Animation Concepts**:
- `layout`: Auto-animates position (great for sortable lists)
- `whileHover`: Hover states (lift effect)
- `whileTap`: Touch feedback
- `initial/animate`: Enter animations

#### Component: League Invite Card

**File**: `web/src/components/leagues/league-invite-card.tsx`

Demonstrates: Copy to clipboard, Web Share API, responsive layout

```typescript
'use client';

import { Share2, Copy, Check, RefreshCw } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useCopyToClipboard } from '@/hooks/use-copy-to-clipboard';
import { useRegenerateInviteCode } from '@/hooks/use-leagues';

interface LeagueInviteCardProps {
  inviteCode: string;
  leagueName: string;
  leagueId: string;
}

export function LeagueInviteCard({ inviteCode, leagueName, leagueId }: LeagueInviteCardProps) {
  const { isCopied, copy } = useCopyToClipboard();
  const regenerateCode = useRegenerateInviteCode(leagueId);

  // Generate shareable URL
  const inviteUrl = typeof window !== 'undefined'
    ? `${window.location.origin}/leagues/join?code=${inviteCode}`
    : '';

  const handleShare = async () => {
    // LEARNING: Web Share API (mobile-first)
    // Check if native sharing is available (mobile devices)
    if (navigator.share) {
      try {
        await navigator.share({
          title: `Join ${leagueName}`,
          text: `Join my betting league: ${leagueName}`,
          url: inviteUrl,
        });
      } catch (err) {
        // User cancelled share dialog - not an error
        if ((err as Error).name !== 'AbortError') {
          copy(inviteUrl);
        }
      }
    } else {
      // Fallback: Copy to clipboard
      copy(inviteUrl);
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Share2 className="size-5" />
            <CardTitle>Invite Friends</CardTitle>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => regenerateCode.mutate()}
            disabled={regenerateCode.isPending}
          >
            <RefreshCw className={`size-4 ${regenerateCode.isPending ? 'animate-spin' : ''}`} />
          </Button>
        </div>
        <CardDescription>
          Share this code or link to invite members
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        {/* Invite Code */}
        <div className="space-y-2">
          <label className="text-sm font-medium">Invite Code</label>
          <div className="flex gap-2">
            <Input
              value={inviteCode}
              readOnly
              className="font-mono text-lg tracking-wider text-center"
            />
            <Button
              variant="outline"
              size="icon"
              onClick={() => copy(inviteCode)}
            >
              {isCopied ? (
                <Check className="size-4 text-green-500" />
              ) : (
                <Copy className="size-4" />
              )}
            </Button>
          </div>
        </div>

        {/* Shareable Link */}
        <div className="space-y-2">
          <label className="text-sm font-medium">Shareable Link</label>
          <div className="flex gap-2">
            <Input
              value={inviteUrl}
              readOnly
              className="text-sm"
            />
            <Button variant="outline" onClick={handleShare}>
              <Share2 className="size-4 mr-2" />
              Share
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
```

**Learning Points**:
- **Progressive Enhancement**: Use native Share API when available, fallback to clipboard
- **Visual Feedback**: Icon changes when copied (Check vs Copy)
- **Loading States**: Spinning icon during regenerate

#### Component: League Members List

**File**: `web/src/components/leagues/league-members-list.tsx`

Demonstrates: List rendering, dropdown menus, role-based UI

```typescript
'use client';

import { MoreVertical, Crown, UserMinus, Shield } from 'lucide-react';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { Button } from '@/components/ui/button';
import { useKickMember } from '@/hooks/use-leagues';
import { useAuthStore } from '@/stores/auth-store';
import type { LeagueMember } from '@/types/leagues';
import { formatDistanceToNow } from 'date-fns';
import { useState } from 'react';

interface LeagueMembersListProps {
  members: LeagueMember[];
  leagueId: string;
  ownerId: string;
}

export function LeagueMembersList({ members, leagueId, ownerId }: LeagueMembersListProps) {
  const currentUser = useAuthStore((state) => state.user);
  const kickMember = useKickMember(leagueId);
  const [kickingUserId, setKickingUserId] = useState<string | null>(null);

  const isOwner = currentUser?.id === ownerId;

  const handleKick = (userId: string) => {
    kickMember.mutate(userId);
    setKickingUserId(null);
  };

  // LEARNING: Find user for confirmation dialog
  const kickingMember = members.find(m => m.userId === kickingUserId);

  return (
    <>
      <div className="space-y-3">
        {members.map((member) => (
          <div
            key={member.userId}
            className="flex items-center justify-between p-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
          >
            <div className="flex items-center gap-3">
              {/* Avatar */}
              <Avatar>
                <AvatarFallback className="bg-primary/10 text-primary font-semibold">
                  {member.username.substring(0, 2).toUpperCase()}
                </AvatarFallback>
              </Avatar>

              {/* User Info */}
              <div>
                <div className="flex items-center gap-2">
                  <p className="font-medium">{member.username}</p>
                  {member.role === 'Owner' && (
                    <Badge variant="secondary" className="gap-1">
                      <Crown className="size-3" />
                      Owner
                    </Badge>
                  )}
                  {member.userId === currentUser?.id && (
                    <Badge variant="outline" className="text-xs">You</Badge>
                  )}
                </div>
                <p className="text-sm text-muted-foreground">
                  Joined {formatDistanceToNow(new Date(member.joinedAt), {
                    addSuffix: true
                  })}
                </p>
              </div>
            </div>

            {/* Owner Actions */}
            {isOwner && member.role !== 'Owner' && member.userId !== currentUser?.id && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon">
                    <MoreVertical className="size-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem
                    className="text-destructive focus:text-destructive"
                    onClick={() => setKickingUserId(member.userId)}
                  >
                    <UserMinus className="size-4 mr-2" />
                    Remove Member
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>
        ))}
      </div>

      {/* Confirmation Dialog */}
      <AlertDialog open={!!kickingUserId} onOpenChange={() => setKickingUserId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove Member?</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to remove <strong>{kickingMember?.username}</strong> from this league?
              They will need a new invite code to rejoin.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => kickingUserId && handleKick(kickingUserId)}
              className="bg-destructive hover:bg-destructive/90"
            >
              Remove
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
```

**Learning Points**:
- **Confirmation Dialogs**: Use AlertDialog for destructive actions
- **Conditional Rendering**: Show actions only for league owner
- **Visual Distinctions**: Badges for roles, "You" badge for current user
- **Hover States**: Subtle background color change

### 6. Pages to Implement

#### Page: Leagues List

**File**: `web/src/app/(protected)/leagues/page.tsx`

Demonstrates: Loading states, empty states, staggered animations

```typescript
'use client';

import { Plus } from 'lucide-react';
import { ProtectedRoute } from '@/components/auth/protected-route';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { CreateLeagueDialog } from '@/components/leagues/create-league-dialog';
import { LeagueCard } from '@/components/leagues/league-card';
import { useLeagues } from '@/hooks/use-leagues';
import { useRouter } from 'next/navigation';
import { motion, AnimatePresence } from 'framer-motion';

function LeaguesContent() {
  const router = useRouter();
  const { data: leagues, isLoading } = useLeagues();

  // LEARNING: Loading State Pattern
  if (isLoading) {
    return (
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <Skeleton key={i} className="h-32 rounded-lg" />
        ))}
      </div>
    );
  }

  // LEARNING: Empty State Pattern
  if (!leagues || leagues.length === 0) {
    return (
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="flex flex-col items-center justify-center py-16 text-center"
      >
        <div className="size-16 rounded-full bg-primary/10 flex items-center justify-center mb-4">
          <Plus className="size-8 text-primary" />
        </div>
        <h3 className="text-2xl font-bold">No Leagues Yet</h3>
        <p className="mt-2 text-muted-foreground max-w-md">
          Create your first betting league to start predicting match results with friends
        </p>
        <CreateLeagueDialog>
          <Button className="mt-6" size="lg">
            <Plus className="mr-2 size-5" />
            Create Your First League
          </Button>
        </CreateLeagueDialog>
      </motion.div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">My Leagues</h1>
          <p className="text-muted-foreground">
            {leagues.length} {leagues.length === 1 ? 'league' : 'leagues'}
          </p>
        </div>
        <CreateLeagueDialog>
          <Button>
            <Plus className="mr-2 size-4" />
            New League
          </Button>
        </CreateLeagueDialog>
      </div>

      {/* LEARNING: Staggered List Animations */}
      <motion.div
        className="grid gap-4 md:grid-cols-2 lg:grid-cols-3"
        initial="hidden"
        animate="visible"
        variants={{
          visible: {
            transition: {
              staggerChildren: 0.05, // Delay between each child
            },
          },
        }}
      >
        <AnimatePresence mode="popLayout">
          {leagues.map((league) => (
            <LeagueCard
              key={league.id}
              league={league}
              onClick={() => router.push(`/leagues/${league.id}`)}
            />
          ))}
        </AnimatePresence>
      </motion.div>
    </div>
  );
}

export default function LeaguesPage() {
  return (
    <ProtectedRoute>
      <div className="container py-8">
        <LeaguesContent />
      </div>
    </ProtectedRoute>
  );
}
```

**Key Patterns**:
- **Three States**: Loading → Empty → Data
- **Staggered Animations**: Children appear one by one
- **AnimatePresence**: Handle component removal animations
- **Responsive Grid**: 1 col mobile, 2 tablet, 3 desktop

#### Page: League Detail

**File**: `web/src/app/(protected)/leagues/[id]/page.tsx`

Demonstrates: Dynamic routes, tabs, dependent queries

```typescript
'use client';

import { use } from 'react';
import { ArrowLeft, Users, Settings as SettingsIcon, Trophy, TrendingUp } from 'lucide-react';
import { ProtectedRoute } from '@/components/auth/protected-route';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Skeleton } from '@/components/ui/skeleton';
import { LeagueInviteCard } from '@/components/leagues/league-invite-card';
import { LeagueMembersList } from '@/components/leagues/league-members-list';
import { useLeague } from '@/hooks/use-leagues';
import { useAuthStore } from '@/stores/auth-store';
import { useRouter } from 'next/navigation';

interface LeagueDetailPageProps {
  params: Promise<{ id: string }>;
}

function LeagueDetailContent({ leagueId }: { leagueId: string }) {
  const router = useRouter();
  const currentUser = useAuthStore((state) => state.user);
  const { data: league, isLoading, error } = useLeague(leagueId);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-20 w-full" />
        <Skeleton className="h-96 w-full" />
      </div>
    );
  }

  if (error || !league) {
    return (
      <div className="text-center py-16">
        <h2 className="text-2xl font-bold">League Not Found</h2>
        <p className="text-muted-foreground mt-2">
          This league doesn't exist or you don't have access.
        </p>
        <Button onClick={() => router.push('/leagues')} className="mt-4">
          Back to Leagues
        </Button>
      </div>
    );
  }

  const isOwner = currentUser?.id === league.ownerId;
  const isMember = league.members.some(m => m.userId === currentUser?.id);

  return (
    <div className="space-y-6">
      {/* Header with Back Button */}
      <div className="flex items-start gap-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => router.back()}
        >
          <ArrowLeft className="size-4" />
        </Button>
        <div className="flex-1">
          <h1 className="text-3xl font-bold">{league.name}</h1>
          {league.description && (
            <p className="mt-2 text-muted-foreground">{league.description}</p>
          )}
          <div className="flex items-center gap-4 mt-3 text-sm text-muted-foreground">
            <span>{league.members.length} members</span>
            <span>•</span>
            <span>Owned by {league.ownerUsername}</span>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="members" className="space-y-6">
        <TabsList>
          <TabsTrigger value="members" className="gap-2">
            <Users className="size-4" />
            Members
          </TabsTrigger>
          <TabsTrigger value="standings" className="gap-2">
            <Trophy className="size-4" />
            Standings
          </TabsTrigger>
          <TabsTrigger value="activity" className="gap-2">
            <TrendingUp className="size-4" />
            Activity
          </TabsTrigger>
          {isOwner && (
            <TabsTrigger value="settings" className="gap-2">
              <SettingsIcon className="size-4" />
              Settings
            </TabsTrigger>
          )}
        </TabsList>

        {/* Members Tab */}
        <TabsContent value="members" className="space-y-6">
          {isOwner && (
            <LeagueInviteCard
              inviteCode={league.inviteCode}
              leagueName={league.name}
              leagueId={league.id}
            />
          )}
          <LeagueMembersList
            members={league.members}
            leagueId={league.id}
            ownerId={league.ownerId}
          />
        </TabsContent>

        {/* Standings Tab */}
        <TabsContent value="standings">
          <p className="text-muted-foreground">
            Leaderboard will be implemented in Phase 6.3
          </p>
        </TabsContent>

        {/* Activity Tab */}
        <TabsContent value="activity">
          <p className="text-muted-foreground">
            Recent bets and results will be shown here
          </p>
        </TabsContent>

        {/* Settings Tab (Owner Only) */}
        {isOwner && (
          <TabsContent value="settings">
            <p className="text-muted-foreground">
              League settings form (similar to create dialog)
            </p>
          </TabsContent>
        )}
      </Tabs>
    </div>
  );
}

export default function LeagueDetailPage({ params }: LeagueDetailPageProps) {
  // LEARNING: use() hook unwraps promises in RSC
  const { id } = use(params);

  return (
    <ProtectedRoute>
      <div className="container max-w-5xl py-8">
        <LeagueDetailContent leagueId={id} />
      </div>
    </ProtectedRoute>
  );
}
```

**Key Concepts**:
- **Dynamic Routes**: `[id]` folder becomes route parameter
- **use() Hook**: Unwraps promise from Next.js params
- **Error States**: Handle not found / unauthorized
- **Conditional Tabs**: Show settings only to owner
- **Back Navigation**: router.back() for better UX

#### Page: Join League

**File**: `web/src/app/(protected)/leagues/join/page.tsx`

Demonstrates: Search params, form pre-fill, redirects

```typescript
'use client';

import { useEffect } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ArrowLeft } from 'lucide-react';
import { ProtectedRoute } from '@/components/auth/protected-route';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { useJoinLeague } from '@/hooks/use-leagues';

const joinLeagueSchema = z.object({
  inviteCode: z.string()
    .min(1, 'Invite code is required')
    .max(50, 'Invalid invite code'),
});

type JoinLeagueFormData = z.infer<typeof joinLeagueSchema>;

function JoinLeagueContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const codeFromUrl = searchParams?.get('code'); // ?code=ABC123

  const joinLeague = useJoinLeague();

  const form = useForm<JoinLeagueFormData>({
    resolver: zodResolver(joinLeagueSchema),
    defaultValues: {
      inviteCode: '',
    },
  });

  // LEARNING: Pre-fill form from URL params
  useEffect(() => {
    if (codeFromUrl) {
      form.setValue('inviteCode', codeFromUrl.toUpperCase());
    }
  }, [codeFromUrl, form]);

  const onSubmit = async (data: JoinLeagueFormData) => {
    // Note: Backend expects leagueId + inviteCode
    // For simplicity, assuming backend has endpoint to join by code alone
    // Or we need to lookup league first, then join
    // This is a design decision to discuss with backend

    try {
      await joinLeague.mutateAsync({
        leagueId: 'lookup-by-code', // TODO: Implement lookup
        inviteCode: data.inviteCode,
      });
      router.push('/leagues');
    } catch (error) {
      // Error handling done in mutation
    }
  };

  return (
    <div className="min-h-[60vh] flex items-center justify-center">
      <Card className="w-full max-w-md">
        <CardHeader>
          <div className="flex items-center gap-3">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => router.back()}
            >
              <ArrowLeft className="size-4" />
            </Button>
            <div>
              <CardTitle>Join a League</CardTitle>
              <CardDescription>
                Enter the invite code shared with you
              </CardDescription>
            </div>
          </div>
        </CardHeader>

        <CardContent>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              <FormField
                control={form.control}
                name="inviteCode"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Invite Code</FormLabel>
                    <FormControl>
                      <Input
                        placeholder="Enter code..."
                        className="font-mono text-lg tracking-wider uppercase"
                        {...field}
                        onChange={(e) => field.onChange(e.target.value.toUpperCase())}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <Button
                type="submit"
                className="w-full"
                disabled={joinLeague.isPending}
              >
                {joinLeague.isPending ? 'Joining...' : 'Join League'}
              </Button>
            </form>
          </Form>
        </CardContent>
      </Card>
    </div>
  );
}

export default function JoinLeaguePage() {
  return (
    <ProtectedRoute>
      <div className="container py-8">
        <JoinLeagueContent />
      </div>
    </ProtectedRoute>
  );
}
```

**Key Patterns**:
- **Search Params**: useSearchParams() for query strings
- **Auto-fill**: Pre-populate form from URL
- **Transform Input**: Auto-uppercase invite codes
- **Centered Layout**: Card centered on page

### 7. Phase 6.1 Learning Summary

| Concept | Where Learned | Why Important |
|---------|---------------|---------------|
| **Query Keys Hierarchy** | `use-leagues.ts` | Enables targeted cache invalidation |
| **Optimistic Updates** | `useCreateLeague` | Instant UI feedback |
| **Dependent Queries** | `useLeague` with `enabled` | Prevent unnecessary API calls |
| **staleTime Strategy** | All queries | Balance freshness vs performance |
| **Zod + RHF** | `create-league-dialog.tsx` | Type-safe form validation |
| **Render Props** | `FormField` | Flexible component composition |
| **Framer Motion Basics** | `LeagueCard` | Smooth animations |
| **Staggered Lists** | `leagues/page.tsx` | Delightful enter animations |
| **Empty States** | `leagues/page.tsx` | Guide users when no data |
| **Loading Skeletons** | All pages | Prevent layout shift |
| **Dynamic Routes** | `leagues/[id]/page.tsx` | URL-based navigation |
| **Search Params** | `leagues/join/page.tsx` | Deep linking support |
| **Web Share API** | `league-invite-card.tsx` | Mobile-first sharing |
| **Confirmation Dialogs** | `league-members-list.tsx` | Prevent accidental actions |
| **Role-Based UI** | League detail page | Conditional features |

### 8. Critical Files for Phase 6.1

| File | Purpose | Key Concepts |
|------|---------|--------------|
| `web/src/types/leagues.ts` | Type definitions | TypeScript interfaces |
| `web/src/hooks/use-leagues.ts` | Data fetching | React Query patterns |
| `web/src/hooks/use-copy-to-clipboard.ts` | Reusable logic | Custom hooks |
| `web/src/components/leagues/create-league-dialog.tsx` | Form handling | Zod + RHF |
| `web/src/components/leagues/league-card.tsx` | List item | Framer Motion |
| `web/src/components/leagues/league-invite-card.tsx` | Sharing | Web APIs |
| `web/src/components/leagues/league-members-list.tsx` | List rendering | Conditional UI |
| `web/src/app/(protected)/leagues/page.tsx` | List page | States & animations |
| `web/src/app/(protected)/leagues/[id]/page.tsx` | Detail page | Dynamic routes |
| `web/src/app/(protected)/leagues/join/page.tsx` | Join flow | Search params |

