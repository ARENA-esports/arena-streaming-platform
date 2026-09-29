import { renderHook, act } from '@testing-library/react';
import { useState } from 'react';

describe('Testing Infrastructure Sanity', () => {
  test('supports renderHook, act, and fake timers', () => {
    jest.useFakeTimers();

    const useCounter = () => {
      const [count, setCount] = useState(0);
      const incrementAfterDelay = () => {
        setTimeout(() => setCount((c) => c + 1), 1000);
      };
      return { count, incrementAfterDelay };
    };

    const { result } = renderHook(() => useCounter());

    expect(result.current.count).toBe(0);

    act(() => {
      result.current.incrementAfterDelay();
    });

    expect(result.current.count).toBe(0);

    act(() => {
      jest.advanceTimersByTime(1000);
    });

    expect(result.current.count).toBe(1);

    jest.useRealTimers();
  });

  test('supports DOM events and visibilityState manipulation', () => {
    const handler = jest.fn();
    document.addEventListener('visibilitychange', handler);

    act(() => {
      Object.defineProperty(document, 'visibilityState', {
        configurable: true,
        value: 'hidden',
      });
      document.dispatchEvent(new Event('visibilitychange'));
    });

    expect(handler).toHaveBeenCalledTimes(1);
    expect(document.visibilityState).toBe('hidden');

    act(() => {
      Object.defineProperty(document, 'visibilityState', {
        configurable: true,
        value: 'visible',
      });
      document.dispatchEvent(new Event('visibilitychange'));
    });

    expect(handler).toHaveBeenCalledTimes(2);
    expect(document.visibilityState).toBe('visible');

    document.removeEventListener('visibilitychange', handler);
  });

  test('supports jest-dom matchers', () => {
    const div = document.createElement('div');
    div.textContent = 'Arena Platform';
    document.body.appendChild(div);
    expect(div).toBeInTheDocument();
    document.body.removeChild(div);
  });
});
