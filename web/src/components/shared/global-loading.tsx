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
    <div className="fixed top-0 left-0 right-0 z-[100] h-1 bg-primary/20 overflow-hidden">
      <div className="h-full w-[30%] bg-primary animate-[loading_1s_ease-in-out_infinite]" />
      <style>{`
        @keyframes loading {
          0% { transform: translateX(-100%); }
          100% { transform: translateX(400%); }
        }
      `}</style>
    </div>
  );
}
