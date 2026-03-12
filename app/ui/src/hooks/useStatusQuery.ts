import { useQuery } from '@tanstack/react-query';
import { fetchStatus } from '../utils/api';
import { saveToDb, loadFromDb } from '../utils/db';
import { POLL_INTERVAL } from '../config/constants';
import type { CachedStatusData } from '../types/api';

export function useStatusQuery(sort: string, page: number, shelters: string[]) {
  const stableShelters = [...shelters].sort();

  return useQuery({
    queryKey: ['status', sort, page, stableShelters] as const,
    queryFn: async (): Promise<CachedStatusData | null> => {
      try {
        const raw = await fetchStatus(sort, page, shelters);
        if ('offline' in raw) {
          const cached = await loadFromDb();
          return cached ? { ...cached, fromCache: true } : null;
        }
        await saveToDb(raw).catch(() => {});
        return { ...raw, fromCache: false };
      } catch {
        const cached = await loadFromDb();
        return cached ? { ...cached, fromCache: true } : null;
      }
    },
    refetchInterval: POLL_INTERVAL,
    refetchOnWindowFocus: true,
    staleTime: 0,
    retry: false,
  });
}
