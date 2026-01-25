import { Skeleton } from '@/components/ui/skeleton';

/**
 * Card skeleton for loading states
 *
 * Why skeletons?
 * - Better perceived performance than spinners
 * - User sees the layout before data loads
 * - Reduces layout shift when data arrives
 */
export function CardSkeleton() {
  return (
    <div className="rounded-lg border bg-card p-4">
      <Skeleton className="h-5 w-2/3 mb-2" />
      <Skeleton className="h-4 w-1/2 mb-4" />
      <Skeleton className="h-4 w-full" />
    </div>
  );
}

/**
 * List of card skeletons
 */
export function CardListSkeleton({ count = 3 }: { count?: number }) {
  return (
    <div className="space-y-4">
      {Array.from({ length: count }).map((_, i) => (
        <CardSkeleton key={i} />
      ))}
    </div>
  );
}

/**
 * Grid of card skeletons
 */
export function CardGridSkeleton({ count = 6 }: { count?: number }) {
  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: count }).map((_, i) => (
        <CardSkeleton key={i} />
      ))}
    </div>
  );
}
