import { createContext, useContext, useState, type ReactNode } from 'react';
import { useLocalStorage } from '../hooks/useLocalStorage';
import { useStatusQuery } from '../hooks/useStatusQuery';
import { useMonitorTrigger } from '../hooks/useMonitorTrigger';
import { usePushNotifications, type PushNotificationsResult } from '../hooks/usePushNotifications';
import { SHELTER_IDS, SORT_KEY, SHELTER_FILTER_KEY } from '../config/constants';
import type { CachedStatusData } from '../types/api';
import type { UseQueryResult } from '@tanstack/react-query';

interface AppContextValue {
  activeTab: 'available' | 'adopted';
  setActiveTab: (tab: 'available' | 'adopted') => void;
  sort: string;
  setSort: (sort: string) => void;
  page: number;
  setPage: (page: number) => void;
  activeShelters: string[];
  setActiveShelters: (shelters: string[]) => void;
  statusQuery: UseQueryResult<CachedStatusData | null>;
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
  const [activeTab, setActiveTab] = useState<'available' | 'adopted'>('available');
  const { value: sort, setValue: setSortValue } = useLocalStorage(SORT_KEY, 'newest');
  const [page, setPage] = useState(1);
  const { value: activeShelters, setValue: setActiveSheltersValue } = useLocalStorage<string[]>(
    SHELTER_FILTER_KEY,
    [...SHELTER_IDS],
  );
  const [isNotifPanelOpen, setIsNotifPanelOpen] = useState(false);
  const [isNotifSetupOpen, setIsNotifSetupOpen] = useState(false);

  const statusQuery = useStatusQuery(sort, page, activeShelters);
  const { monitorError, forceMonitor } = useMonitorTrigger(statusQuery.refetch);
  const push = usePushNotifications();

  const setSort = (newSort: string) => {
    setSortValue(newSort);
    setPage(1);
  };

  const setActiveShelters = (shelters: string[]) => {
    setActiveSheltersValue(shelters);
    setPage(1);
  };

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
