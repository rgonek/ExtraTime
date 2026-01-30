'use client';

import { BotStrategy } from '@/types';

const strategyInfo: Record<BotStrategy, { icon: string; label: string; description: string }> = {
  Random: {
    icon: '🎲',
    label: 'Random',
    description: 'Makes random predictions'
  },
  HomeFavorer: {
    icon: '🏠',
    label: 'Home Favorer',
    description: 'Always backs the home team'
  },
  UnderdogSupporter: {
    icon: '🐕',
    label: 'Underdog',
    description: 'Loves an upset'
  },
  DrawPredictor: {
    icon: '🤝',
    label: 'Draw Expert',
    description: 'Expects stalemates'
  },
  HighScorer: {
    icon: '⚽',
    label: 'High Scorer',
    description: 'Predicts lots of goals'
  },
  StatsAnalyst: {
    icon: '🧠',
    label: 'Stats Analyst',
    description: 'Uses statistical analysis'
  }
};

interface BotStrategyInfoProps {
  strategy: BotStrategy;
}

export function BotStrategyInfo({ strategy }: BotStrategyInfoProps) {
  const info = strategyInfo[strategy];
  return (
    <div className="flex items-center gap-2">
      <span className="text-lg">{info.icon}</span>
      <div>
        <p className="font-medium">{info.label}</p>
        <p className="text-sm text-muted-foreground">{info.description}</p>
      </div>
    </div>
  );
}

export function BotIndicator({ strategy }: BotStrategyInfoProps) {
  const info = strategyInfo[strategy];
  return (
    <span className="text-lg" title={info.label}>
      {info.icon}
    </span>
  );
}
