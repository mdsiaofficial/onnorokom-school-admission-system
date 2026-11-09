import { useState, useEffect } from 'react';
import { useClockStore } from '@/stores/clockStore';

export interface CountdownResult {
  remainingMs: number;
  remainingSeconds: number;
  isExpired: boolean;
  formatted: string;
}

export const calculateRemaining = (
  expiresAtIso: string,
  serverOffsetMs: number
): CountdownResult => {
  try {
    const expireTime = new Date(expiresAtIso).getTime();
    if (Number.isNaN(expireTime)) {
      return { remainingMs: 0, remainingSeconds: 0, isExpired: true, formatted: '00:00' };
    }

    const currentSynchronizedTime = Date.now() + serverOffsetMs;
    const diff = expireTime - currentSynchronizedTime;

    if (diff <= 0) {
      return { remainingMs: 0, remainingSeconds: 0, isExpired: true, formatted: '00:00' };
    }

    const totalSeconds = Math.floor(diff / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    const formattedMinutes = String(minutes).padStart(2, '0');
    const formattedSeconds = String(seconds).padStart(2, '0');

    return {
      remainingMs: diff,
      remainingSeconds: totalSeconds,
      isExpired: false,
      formatted: `${formattedMinutes}:${formattedSeconds}`,
    };
  } catch {
    return { remainingMs: 0, remainingSeconds: 0, isExpired: true, formatted: '00:00' };
  }
};

/**
 * React hook that ticks every second and provides remaining countdown.
 * When it transitions to expired, triggers onExpire callback.
 */
export const useCountdown = (
  expiresAtIso: string,
  onExpire?: () => void
): CountdownResult => {
  const serverOffsetMs = useClockStore((s) => s.serverOffsetMs);
  const [result, setResult] = useState<CountdownResult>(() =>
    calculateRemaining(expiresAtIso, serverOffsetMs)
  );

  useEffect(() => {
    // Initial check
    const initial = calculateRemaining(expiresAtIso, serverOffsetMs);
    setResult(initial);
    if (initial.isExpired) {
      return;
    }

    const interval = setInterval(() => {
      const updated = calculateRemaining(expiresAtIso, serverOffsetMs);
      setResult(updated);

      if (updated.isExpired) {
        clearInterval(interval);
        if (onExpire) {
          onExpire();
        }
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [expiresAtIso, serverOffsetMs, onExpire]);

  return result;
};
