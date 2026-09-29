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
  const [isHighlighted, setIsHighlighted] = useState(false);
  const prevBalanceRef = useRef<number | null>(null);
  const isHydratedRef = useRef(false);
  const dismissTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (isLoading) {
      return;
    }

    // First time balance is loaded from the context -> record baseline hydration
    if (!isHydratedRef.current) {
      isHydratedRef.current = true;
      prevBalanceRef.current = balance;
      return;
    }

    if (prevBalanceRef.current !== null) {
      const diff = balance - prevBalanceRef.current;
      if (diff > 0) {
        setIsHighlighted(true);

        if (dismissTimerRef.current) {
          clearTimeout(dismissTimerRef.current);
        }

        dismissTimerRef.current = setTimeout(() => {
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
        <Coins size={14} className="text-amber-400/60 animate-pulse" />
        <span className="w-8 h-4 bg-[var(--line)] rounded animate-pulse"></span>
      </div>
    );
  }

  return (
    <div className="relative inline-flex items-center">
      <div
        className={`flex items-center gap-1.5 px-3 py-1 bg-[var(--panel)] rounded-full text-amber-400 text-sm font-bold transition-all duration-300 ${isHighlighted
            ? 'ring-2 ring-amber-400/50 shadow-[0_0_12px_rgba(251,191,36,0.4)] scale-105'
            : ''
          }`}
        title="Current Coin Balance"
      >
        <Coins size={14} className="text-amber-400" />
        <span>{animatedBalance.toLocaleString()}</span>
      </div>
    </div>
  );
};

export default WalletBalance;
