import { createContext, useContext, useState, useMemo, useEffect, type ReactNode } from 'react';
import { useLocalStorage } from '../hooks/useLocalStorage';
import { useStatusQuery } from '../hooks/useStatusQuery';
import { useMonitorTrigger } from '../hooks/useMonitorTrigger';
import { usePushNotifications, type PushNotificationsResult } from '../hooks/usePushNotifications';
import { useFavorites, compositeKey, type FavoritesResult } from '../hooks/useFavorites';
import { SHELTER_IDS, SORT_KEY, SHELTER_FILTER_KEY, PAGE_SIZE } from '../config/constants';
import { sortAndFilterDogs } from '../utils/sortDogs';
import type { CachedStatusData, DogDto, AdoptedDogDto } from '../types/api';
import type { UseQueryResult } from '@tanstack/react-query';

export type ActiveTab = 'available' | 'favorites' | 'adopted';

interface AppContextValue {
  activeTab: ActiveTab;
  setActiveTab: (tab: ActiveTab) => void;
  sort: string;
  setSort: (sort: string) => void;
  page: number;
  setPage: (page: number) => void;
  activeShelters: string[];
  setActiveShelters: (shelters: string[]) => void;
  statusQuery: UseQueryResult<CachedStatusData | null>;
  visibleDogs: DogDto[];
  totalCount: number;
  favoriteDogs: (DogDto | AdoptedDogDto)[];
  favoriteKeys: Set<string>;
  toggleFavorite: FavoritesResult['toggleFavorite'];
  isFavorite: FavoritesResult['isFavorite'];
  monitorError: string | null;
  forceMonitor: () => void;
  push: PushNotificationsResult;
  isNotifPanelOpen: boolean;
  setIsNotifPanelOpen: (open: boolean) => void;
  isNotifSetupOpen: boolean;
  setIsNotifSetupOpen: (open: boolean) => void;
}

const AppContext = createContext<AppContextValue | null>(null);

export function AppProvider({ children }: { children: ReactNode }) {
  const [activeTab, setActiveTab] = useState<ActiveTab>('available');
  const { value: sort, setValue: setSortValue } = useLocalStorage(SORT_KEY, 'newest');
  const [page, setPage] = useState(1);
  const { value: activeShelters, setValue: setActiveSheltersValue } = useLocalStorage<string[]>(
    SHELTER_FILTER_KEY,
    [...SHELTER_IDS],
  );
  const [isNotifPanelOpen, setIsNotifPanelOpen] = useState(false);
  const [isNotifSetupOpen, setIsNotifSetupOpen] = useState(false);

  const statusQuery = useStatusQuery();
  const { monitorError, forceMonitor } = useMonitorTrigger(statusQuery.refetch);
  const push = usePushNotifications();
  const { favoriteKeys, toggleFavorite, isFavorite, pruneToKeys } = useFavorites();

  const setSort = (newSort: string) => {
    setSortValue(newSort);
    setPage(1);
  };

  const setActiveShelters = (shelters: string[]) => {
    setActiveSheltersValue(shelters);
    setPage(1);
  };

  const allDogs = statusQuery.data?.dogs ?? [];
  const allAdopted = statusQuery.data?.recentlyAdopted ?? [];

  useEffect(() => {
    if (!statusQuery.data) return;
    const validKeys = new Set([
      ...allDogs.map(compositeKey),
      ...allAdopted.map(compositeKey),
    ]);
    pruneToKeys(validKeys);
  }, [statusQuery.data]);

  const sortedFiltered = useMemo(
    () => sortAndFilterDogs(allDogs, sort, activeShelters),
    [allDogs, sort, activeShelters],
  );

  const totalCount = sortedFiltered.length;

  const visibleDogs = useMemo(
    () => sortedFiltered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE),
    [sortedFiltered, page],
  );

  const favoriteDogs = useMemo<(DogDto | AdoptedDogDto)[]>(() => {
    const all: (DogDto | AdoptedDogDto)[] = [...allDogs, ...allAdopted];
    return all.filter((d) => favoriteKeys.has(compositeKey(d)));
  }, [allDogs, allAdopted, favoriteKeys]);

  return (
    <AppContext.Provider
      value={{
        activeTab,
        setActiveTab,
        sort,
        setSort,
        page,
        setPage,
        activeShelters,
        setActiveShelters,
        statusQuery,
        visibleDogs,
        totalCount,
        favoriteDogs,
        favoriteKeys,
        toggleFavorite,
        isFavorite,
        monitorError,
        forceMonitor,
        push,
        isNotifPanelOpen,
        setIsNotifPanelOpen,
        isNotifSetupOpen,
        setIsNotifSetupOpen,
      }}
    >
      {children}
    </AppContext.Provider>
  );
}

export function useAppContext(): AppContextValue {
  const ctx = useContext(AppContext);
  if (!ctx) throw new Error('useAppContext must be used within AppProvider');
  return ctx;
}
