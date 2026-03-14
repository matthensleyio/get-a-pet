import { createContext, useContext, useState, useMemo, useEffect, useCallback, type ReactNode } from 'react';
import { useLocalStorage } from '../hooks/useLocalStorage';
import { useStatusQuery } from '../hooks/useStatusQuery';
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
  setPage: React.Dispatch<React.SetStateAction<number>>;
  activeShelters: string[];
  setActiveShelters: (shelters: string[]) => void;
  statusQuery: UseQueryResult<CachedStatusData | null>;
  visibleDogs: DogDto[];
  visibleFavoriteDogs: (DogDto | AdoptedDogDto)[];
  visibleAdoptedDogs: AdoptedDogDto[];
  scrollPosition: number;
  setScrollPosition: (pos: number) => void;
  totalCount: number;
  favoriteDogs: (DogDto | AdoptedDogDto)[];
  favoriteKeys: Set<string>;
  toggleFavorite: FavoritesResult['toggleFavorite'];
  isFavorite: FavoritesResult['isFavorite'];
  push: PushNotificationsResult;
  isNotifPanelOpen: boolean;
  setIsNotifPanelOpen: (open: boolean) => void;
  isNotifSetupOpen: boolean;
  setIsNotifSetupOpen: (open: boolean) => void;
}

const AppContext = createContext<AppContextValue | null>(null);

export function AppProvider({ children }: { children: ReactNode }) {
  const [activeTab, setActiveTabState] = useState<ActiveTab>('available');
  const { value: sort, setValue: setSortValue } = useLocalStorage(SORT_KEY, 'newest');
  const [page, setPage] = useState(2);
  const { value: activeShelters, setValue: setActiveSheltersValue } = useLocalStorage<string[]>(
    SHELTER_FILTER_KEY,
    [...SHELTER_IDS],
  );
  const [scrollPosition, setScrollPosition] = useState(0);
  const [isNotifPanelOpen, setIsNotifPanelOpen] = useState(false);
  const [isNotifSetupOpen, setIsNotifSetupOpen] = useState(false);

  const statusQuery = useStatusQuery();
  const push = usePushNotifications();
  const { favoriteKeys, toggleFavorite, isFavorite, pruneToKeys } = useFavorites();

  const setActiveTab = useCallback((tab: ActiveTab) => {
    setActiveTabState(tab);
    setPage(2);
    window.scrollTo(0, 0);
  }, []);

  const setSort = useCallback((newSort: string) => {
    setSortValue(newSort);
    setPage(2);
  }, [setSortValue]);

  const setActiveShelters = useCallback((shelters: string[]) => {
    setActiveSheltersValue(shelters);
    setPage(2);
  }, [setActiveSheltersValue]);

  const allDogs = useMemo(() => statusQuery.data?.dogs ?? [], [statusQuery.data]);
  const allAdopted = useMemo(() => statusQuery.data?.recentlyAdopted ?? [], [statusQuery.data]);

  useEffect(() => {
    if (!statusQuery.data) return;
    const validKeys = new Set([
      ...allDogs.map(compositeKey),
      ...allAdopted.map(compositeKey),
    ]);
    pruneToKeys(validKeys);
  }, [statusQuery.data, allDogs, allAdopted, pruneToKeys]);

  const sortedFiltered = useMemo(
    () => sortAndFilterDogs(allDogs, sort, activeShelters),
    [allDogs, sort, activeShelters],
  );

  const totalCount = sortedFiltered.length;

  const visibleDogs = useMemo(
    () => sortedFiltered.slice(0, page * PAGE_SIZE),
    [sortedFiltered, page],
  );

  const favoriteDogs = useMemo<(DogDto | AdoptedDogDto)[]>(() => {
    const all: (DogDto | AdoptedDogDto)[] = [...allDogs, ...allAdopted];
    const filtered = all.filter((d) => favoriteKeys.has(compositeKey(d)));
    return filtered.sort((a, b) => {
      const aAdopted = 'adoptedAt' in a ? 1 : 0;
      const bAdopted = 'adoptedAt' in b ? 1 : 0;
      return aAdopted - bAdopted;
    });
  }, [allDogs, allAdopted, favoriteKeys]);

  const visibleFavoriteDogs = useMemo(
    () => favoriteDogs.slice(0, page * PAGE_SIZE),
    [favoriteDogs, page],
  );

  const sortedAdopted = useMemo(
    () =>
      [...allAdopted].sort(
        (a: AdoptedDogDto, b: AdoptedDogDto) =>
          new Date(b.adoptedAt).getTime() - new Date(a.adoptedAt).getTime(),
      ),
    [allAdopted],
  );

  const visibleAdoptedDogs = useMemo(
    () => sortedAdopted.slice(0, page * PAGE_SIZE),
    [sortedAdopted, page],
  );

  const value = useMemo(
    () => ({
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
      visibleFavoriteDogs,
      visibleAdoptedDogs,
      scrollPosition,
      setScrollPosition,
      totalCount,
      favoriteDogs,
      favoriteKeys,
      toggleFavorite,
      isFavorite,
      push,
      isNotifPanelOpen,
      setIsNotifPanelOpen,
      isNotifSetupOpen,
      setIsNotifSetupOpen,
    }),
    [
      activeTab,
      setActiveTab,
      sort,
      setSort,
      page,
      activeShelters,
      setActiveShelters,
      statusQuery,
      visibleDogs,
      visibleFavoriteDogs,
      visibleAdoptedDogs,
      scrollPosition,
      totalCount,
      favoriteDogs,
      favoriteKeys,
      toggleFavorite,
      isFavorite,
      push,
      isNotifPanelOpen,
      isNotifSetupOpen,
    ],
  );

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useAppContext(): AppContextValue {
  const ctx = useContext(AppContext);
  if (!ctx) throw new Error('useAppContext must be used within AppProvider');
  return ctx;
}
