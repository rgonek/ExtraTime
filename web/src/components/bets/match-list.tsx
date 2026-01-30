'use client';

import { useMemo, useState } from 'react';
import { Calendar } from 'lucide-react';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useMatches, useCompetitions } from '@/hooks/use-matches';
import { useMyBets } from '@/hooks/use-bets';
import { useLeague } from '@/hooks/use-leagues';
import { MatchCard } from './match-card';
import { CardListSkeleton } from '@/components/shared/loading-skeleton';
import { EmptyState } from '@/components/shared/empty-state';
import { ErrorMessage } from '@/components/shared/error-message';
import type { MatchDto, MyBetDto } from '@/types';

interface MatchListProps {
  leagueId: string;
}

/**
 * List of matches with filtering and grouping
 *
 * Demonstrates:
 * - useMemo for expensive calculations
 * - Filtering and grouping data
 * - Combining multiple queries
 */
export function MatchList({ leagueId }: MatchListProps) {
  const [selectedCompetition, setSelectedCompetition] = useState<string>('all');
  const [showTab, setShowTab] = useState<'upcoming' | 'past'>('upcoming');

  // Fetch data from multiple sources
  const { data: league, isLoading: leagueLoading } = useLeague(leagueId);
  const { data: myBets, isLoading: betsLoading } = useMyBets(leagueId);
  const { data: matchesResponse, isLoading: matchesLoading, isError, error, refetch } =
    useMatches({ pageSize: 100 }); // Get more matches
  const { data: competitions } = useCompetitions();

  const isLoading = leagueLoading || betsLoading || matchesLoading;

  // ============================================================
  // useMemo: MEMOIZE EXPENSIVE CALCULATIONS
  // ============================================================
  // useMemo caches the result of a calculation.
  // Only recalculates when dependencies change.
  //
  // Backend analogy: Cached computed property that recalculates
  // only when underlying data changes.
  // ============================================================

  // Create bet lookup map for O(1) access
  const betsByMatchId = useMemo(() => {
    if (!myBets) return new Map<string, MyBetDto>();
    return new Map(myBets.map((bet) => [bet.matchId, bet]));
  }, [myBets]);

  // Filter matches by allowed competitions and selected filter
  const filteredMatches = useMemo(() => {
    if (!matchesResponse?.items || !league) return [];

    let matches = matchesResponse.items;

    // Filter by league's allowed competitions
    if (league.allowedCompetitionIds.length > 0) {
      matches = matches.filter((m) =>
        league.allowedCompetitionIds.includes(m.competition.id)
      );
    }

    // Filter by selected competition
    if (selectedCompetition !== 'all') {
      matches = matches.filter((m) => m.competition.id === selectedCompetition);
    }

    return matches;
  }, [matchesResponse?.items, league, selectedCompetition]);

  // Split into upcoming and past matches
  const { upcomingMatches, pastMatches } = useMemo(() => {
    const now = new Date();
    const upcoming: MatchDto[] = [];
    const past: MatchDto[] = [];

    filteredMatches.forEach((match) => {
      if (match.status === 'Finished' || new Date(match.matchDateUtc) < now) {
        past.push(match);
      } else {
        upcoming.push(match);
      }
    });

    // Sort: upcoming by date ascending, past by date descending
    upcoming.sort(
      (a, b) =>
        new Date(a.matchDateUtc).getTime() - new Date(b.matchDateUtc).getTime()
    );
    past.sort(
      (a, b) =>
        new Date(b.matchDateUtc).getTime() - new Date(a.matchDateUtc).getTime()
    );

    return { upcomingMatches: upcoming, pastMatches: past };
  }, [filteredMatches]);

  // Group matches by date for display
  const groupMatchesByDate = (matches: MatchDto[]) => {
    const groups = new Map<string, MatchDto[]>();

    matches.forEach((match) => {
      const dateKey = new Date(match.matchDateUtc).toLocaleDateString(undefined, {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric',
      });

      if (!groups.has(dateKey)) {
        groups.set(dateKey, []);
      }
      groups.get(dateKey)!.push(match);
    });

    return Array.from(groups.entries());
  };

  // Loading state
  if (isLoading) {
    return <CardListSkeleton count={5} />;
  }

  // Error state
  if (isError) {
    return (
      <ErrorMessage
        title="Failed to load matches"
        message={error?.message ?? 'Something went wrong'}
        onRetry={() => refetch()}
      />
    );
  }

  const currentMatches = showTab === 'upcoming' ? upcomingMatches : pastMatches;
  const groupedMatches = groupMatchesByDate(currentMatches);

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <Tabs value={showTab} onValueChange={(v) => setShowTab(v as 'upcoming' | 'past')}>
          <TabsList>
            <TabsTrigger value="upcoming">
              Upcoming ({upcomingMatches.length})
            </TabsTrigger>
            <TabsTrigger value="past">
              Past ({pastMatches.length})
            </TabsTrigger>
          </TabsList>
        </Tabs>

        <Select value={selectedCompetition} onValueChange={setSelectedCompetition}>
          <SelectTrigger className="w-[200px]">
            <SelectValue placeholder="All competitions" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All competitions</SelectItem>
            {competitions?.map((comp) => (
              <SelectItem key={comp.id} value={comp.id}>
                {comp.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Matches grouped by date */}
      {groupedMatches.length === 0 ? (
        <EmptyState
          icon={Calendar}
          title={showTab === 'upcoming' ? 'No upcoming matches' : 'No past matches'}
          description={
            showTab === 'upcoming'
              ? 'Check back later for new matches'
              : 'Past matches will appear here'
          }
        />
      ) : (
        <div className="space-y-6">
          {groupedMatches.map(([date, matches]) => (
            <div key={date}>
              <h3 className="text-sm font-medium text-muted-foreground mb-2">
                {date}
              </h3>
              <div className="space-y-2">
                {matches.map((match) => (
                  <MatchCard
                    key={match.id}
                    match={match}
                    leagueId={leagueId}
                    existingBet={betsByMatchId.get(match.id)}
                    bettingDeadlineMinutes={league?.bettingDeadlineMinutes ?? 60}
                  />
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
