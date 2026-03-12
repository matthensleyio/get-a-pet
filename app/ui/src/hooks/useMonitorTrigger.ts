import { useCallback, useEffect, useRef, useState } from 'react';
import { triggerMonitor } from '../utils/api';
import { MONITOR_INTERVAL } from '../config/constants';

export interface MonitorTriggerResult {
  monitorError: string | null;
  forceMonitor: () => void;
}

export function useMonitorTrigger(refetch: () => void): MonitorTriggerResult {
  const [monitorError, setMonitorError] = useState<string | null>(null);
  const refetchRef = useRef(refetch);

  useEffect(() => {
    refetchRef.current = refetch;
  });

  useEffect(() => {
    let timerId: ReturnType<typeof setInterval> | null = null;

    const run = async () => {
      if (document.visibilityState === 'hidden') return;
      try {
        const result = await triggerMonitor();
        setMonitorError(null);
        if (!result.offline) {
          refetchRef.current();
        }
      } catch (err) {
        setMonitorError(err instanceof Error ? err.message : 'Monitor check failed');
      }
    };

    const start = () => {
      void run();
      timerId = setInterval(() => void run(), MONITOR_INTERVAL);
    };

    const stop = () => {
      if (timerId !== null) {
        clearInterval(timerId);
        timerId = null;
      }
    };

    start();

    const handleVisibility = () => {
      if (document.visibilityState === 'hidden') {
        stop();
      } else {
        stop();
        start();
      }
    };

    document.addEventListener('visibilitychange', handleVisibility);
    return () => {
      stop();
      document.removeEventListener('visibilitychange', handleVisibility);
    };
  }, []);

  const forceMonitor = useCallback(() => {
    void (async () => {
      try {
        await triggerMonitor(true);
        setMonitorError(null);
        refetchRef.current();
      } catch (err) {
        setMonitorError(err instanceof Error ? err.message : 'Monitor check failed');
      }
    })();
  }, []);

  return { monitorError, forceMonitor };
}
