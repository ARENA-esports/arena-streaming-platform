import React, { useEffect, useState, useRef } from 'react';
import { useWallet } from '../../context/WalletContext';
import { Coins } from 'lucide-react';

const useAnimatedNumber = (value: number, duration: number = 800) => {
  const [displayValue, setDisplayValue] = useState(value);
  const startValue = useRef<number>(value);
  const startTime = useRef<number | null>(null);

  useEffect(() => {
    // Prevent animation on initial hydration if it's identical
    if (value === displayValue) return;

    startValue.current = displayValue;
    startTime.current = null;
    let animationFrameId: number;

    const animate = (timestamp: number) => {
      if (!startTime.current) startTime.current = timestamp;
      const progress = timestamp - startTime.current;
      
      if (progress < duration) {
        const easeOutQuart = 1 - Math.pow(1 - progress / duration, 4);
        const nextValue = startValue.current + ((value - startValue.current) * easeOutQuart);
        setDisplayValue(nextValue);
        animationFrameId = requestAnimationFrame(animate);
      } else {
        setDisplayValue(value);
      }
    };

    animationFrameId = requestAnimationFrame(animate);
    return () => cancelAnimationFrame(animationFrameId);
  }, [value, duration]);

  return Math.round(displayValue);
};

export const WalletBalance: React.FC = () => {
  const { balance, isLoading } = useWallet();
  const animatedBalance = useAnimatedNumber(balance);
  const [rewardDelta, setRewardDelta] = useState<number | null>(null);
  const [isHighlighted, setIsHighlighted] = useState(false);
  const prevBalanceRef = useRef<number | null>(null);
  const isHydratedRef = useRef(false);
  const dismissTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (isLoading) {
      return;
    }

    // First time balance is loaded from the context -> record baseline hydration without showing reward badge
    if (!isHydratedRef.current) {
      isHydratedRef.current = true;
      prevBalanceRef.current = balance;
      return;
    }

    if (prevBalanceRef.current !== null) {
      const diff = balance - prevBalanceRef.current;
      if (diff > 0) {
        // Balance increased (e.g. watch reward tick) -> show transient floating badge
        setRewardDelta(diff);
        setIsHighlighted(true);

        if (dismissTimerRef.current) {
          clearTimeout(dismissTimerRef.current);
        }

        dismissTimerRef.current = setTimeout(() => {
          setRewardDelta(null);
          setIsHighlighted(false);
        }, 1200);
      }
    }

    prevBalanceRef.current = balance;
  }, [balance, isLoading]);

  useEffect(() => {
    return () => {
      if (dismissTimerRef.current) {
        clearTimeout(dismissTimerRef.current);
      }
    };
  }, []);

  if (isLoading) {
    return (
      <div className="flex items-center gap-1.5 px-3 py-1 bg-[var(--panel)] border border-[var(--line)] rounded-full text-arena-textMuted text-sm font-bold opacity-70" title="Loading balance...">
        <Coins size={14} className="animate-pulse" />
        <span className="w-8 h-4 bg-[var(--line)] rounded animate-pulse"></span>
      </div>
    );
  }

  return (
    <div className="relative inline-flex items-center">
      <div
        className={`flex items-center gap-1.5 px-3 py-1 bg-[var(--panel)] border rounded-full text-[var(--prime)] text-sm font-bold transition-all duration-300 ${
          isHighlighted
            ? 'border-[var(--prime)] ring-2 ring-[var(--prime)]/50 shadow-[0_0_12px_rgba(0,184,252,0.4)] scale-105'
            : 'border-[var(--prime)]/50'
        }`}
        title="Current Coin Balance"
      >
        <Coins size={14} />
        <span>{animatedBalance.toLocaleString()}</span>
      </div>

      {rewardDelta !== null && (
        <div
          data-testid="wallet-reward-badge"
          className="absolute -top-7 right-0 pointer-events-none px-2 py-0.5 rounded-full bg-[var(--panel)] border border-[var(--prime)]/80 text-[var(--prime)] text-xs font-bold shadow-[0_0_10px_rgba(0,184,252,0.4)] whitespace-nowrap animate-coin-float z-20"
        >
          +{rewardDelta} Coins
        </div>
      )}
    </div>
  );
};

export default WalletBalance;
