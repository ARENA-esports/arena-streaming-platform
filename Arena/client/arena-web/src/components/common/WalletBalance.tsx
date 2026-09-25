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

  if (isLoading) {
    return (
      <div className="flex items-center gap-1.5 px-3 py-1 bg-[var(--panel)] border border-[var(--line)] rounded-full text-arena-textMuted text-sm font-bold opacity-70" title="Loading balance...">
        <Coins size={14} className="animate-pulse" />
        <span className="w-8 h-4 bg-[var(--line)] rounded animate-pulse"></span>
      </div>
    );
  }

  return (
    <div 
      className="flex items-center gap-1.5 px-3 py-1 bg-[var(--panel)] border border-[var(--prime)]/50 rounded-full text-[var(--prime)] text-sm font-bold transition-all"
      title="Current Coin Balance"
    >
      <Coins size={14} />
      <span>{animatedBalance.toLocaleString()}</span>
    </div>
  );
};

export default WalletBalance;
