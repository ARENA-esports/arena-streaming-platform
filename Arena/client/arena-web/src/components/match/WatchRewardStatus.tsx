import React from 'react';
import { Coins, Pause, EyeOff, AlertCircle, Ban } from 'lucide-react';

export type WatchRewardState =
  | 'earning'
  | 'paused'
  | 'tab-inactive'
  | 'capped'
  | 'inactive';

export interface WatchRewardStatusProps {
  status?: WatchRewardState;
  isViewer?: boolean;
  isLiveMatch?: boolean;
  hasValidStream?: boolean;
  isPlaying?: boolean;
  isDocumentVisible?: boolean;
  isCapped?: boolean;
  isStreamNotLive?: boolean;
  className?: string;
}

export const WatchRewardStatus: React.FC<WatchRewardStatusProps> = ({
  status,
  isViewer = true,
  isLiveMatch = true,
  hasValidStream = true,
  isPlaying = true,
  isDocumentVisible = true,
  isCapped = false,
  isStreamNotLive = false,
  className = '',
}) => {
  const resolvedState: WatchRewardState = (() => {
    if (status) return status;
    if (!isViewer || !isLiveMatch || !hasValidStream || isStreamNotLive) {
      return 'inactive';
    }
    if (isCapped) {
      return 'capped';
    }
    if (!isDocumentVisible) {
      return 'tab-inactive';
    }
    if (!isPlaying) {
      return 'paused';
    }
    return 'earning';
  })();

  const config = {
    earning: {
      label: 'Earning Coins',
      containerClass: 'bg-[var(--panel)] border border-[var(--prime)]/50 text-[var(--prime)] shadow-[0_0_10px_rgba(0,184,252,0.15)]',
      dotClass: 'bg-[var(--prime)] animate-pulse shadow-[0_0_6px_var(--prime)]',
      icon: <Coins size={12} className="text-[var(--prime)]" />,
      title: 'Actively earning watch rewards',
    },
    paused: {
      label: 'Rewards Paused',
      containerClass: 'bg-[var(--panel)] border border-amber-500/40 text-amber-400',
      dotClass: 'bg-amber-400',
      icon: <Pause size={12} className="text-amber-400" />,
      title: 'Playback paused. Resume stream to earn coins.',
    },
    'tab-inactive': {
      label: 'Rewards Paused / Tab Inactive',
      containerClass: 'bg-[var(--panel)] border border-[var(--line)] text-arena-textMuted',
      dotClass: 'bg-zinc-500',
      icon: <EyeOff size={12} className="text-arena-textMuted" />,
      title: 'Tab inactive. Return to tab to earn coins.',
    },
    capped: {
      label: 'Reward Cap Reached',
      containerClass: 'bg-[var(--panel)] border border-amber-500/50 text-amber-300',
      dotClass: 'bg-amber-400',
      icon: <AlertCircle size={12} className="text-amber-400" />,
      title: 'Reward cap reached for this stream window.',
    },
    inactive: {
      label: 'Rewards Inactive',
      containerClass: 'bg-[var(--panel)] border border-[var(--line)] text-arena-textMuted opacity-75',
      dotClass: 'bg-zinc-600',
      icon: <Ban size={12} className="text-arena-textMuted" />,
      title: 'Watch rewards currently inactive',
    },
  }[resolvedState];

  return (
    <div
      data-testid="watch-reward-status"
      data-status={resolvedState}
      title={config.title}
      className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-bold tracking-wide transition-all select-none ${config.containerClass} ${className}`}
    >
      <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${config.dotClass}`} />
      {config.icon}
      <span>{config.label}</span>
    </div>
  );
};

export default WatchRewardStatus;
