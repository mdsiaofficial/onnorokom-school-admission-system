import { create } from 'zustand';

interface ClockState {
  serverOffsetMs: number;
  updateOffsetFromDateHeader: (dateHeader: string) => void;
  getSynchronizedNow: () => number;
}

export const useClockStore = create<ClockState>()((set, get) => ({
  serverOffsetMs: 0,
  updateOffsetFromDateHeader: (dateHeader: string) => {
    try {
      const serverTime = new Date(dateHeader).getTime();
      if (!Number.isNaN(serverTime)) {
        const clientTime = Date.now();
        const offset = serverTime - clientTime;
        // Dampen dramatic jumps, but ensure we stay aligned with server clock
        set({ serverOffsetMs: offset });
      }
    } catch {
      // Ignore parse failure
    }
  },
  getSynchronizedNow: () => {
    return Date.now() + get().serverOffsetMs;
  },
}));
