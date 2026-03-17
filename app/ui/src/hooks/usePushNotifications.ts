import { useCallback, useEffect, useState } from 'react';
import { fetchVapidKey, subscribePush, unsubscribePush } from '../utils/api';
import { urlBase64ToUint8Array } from '../utils/urlBase64ToUint8Array';
import { NOTIF_SHELTER_KEY } from '../config/constants';

export interface PushSubData {
  endpoint: string;
  keys: { p256dh: string; auth: string };
}

export interface PushNotificationsResult {
  isSubscribed: boolean;
  isSupported: boolean;
  isIos: boolean;
  isStandalone: boolean;
  notifShelters: string[];
  currentSubData: PushSubData | null;
  subscribe: (shelterIds: string[]) => Promise<void>;
  unsubscribe: () => Promise<void>;
  updateShelters: (shelterIds: string[]) => Promise<void>;
}

function loadNotifShelters(): string[] {
  try {
    const saved = JSON.parse(localStorage.getItem(NOTIF_SHELTER_KEY) ?? 'null') as unknown;
    return Array.isArray(saved) ? (saved as string[]) : [];
  } catch {
    return [];
  }
}

function getSubData(sub: PushSubscription): PushSubData {
  const subJson = sub.toJSON();
  return {
    endpoint: sub.endpoint,
    keys: {
      p256dh: subJson.keys?.['p256dh'] ?? '',
      auth: subJson.keys?.['auth'] ?? '',
    },
  };
}

export function usePushNotifications(): PushNotificationsResult {
  const [isSubscribed, setIsSubscribed] = useState(false);
  const [currentSubData, setCurrentSubData] = useState<PushSubData | null>(null);
  const [notifShelters, setNotifShelters] = useState<string[]>(loadNotifShelters);

  const isSupported = 'serviceWorker' in navigator && 'PushManager' in window;
  const isIos = /iphone|ipad|ipod/i.test(navigator.userAgent);
  const isStandalone =
    (navigator as Navigator & { standalone?: boolean }).standalone === true;

  useEffect(() => {
    if (!isSupported) return;
    void navigator.serviceWorker.ready.then((reg) => {
      void reg.pushManager.getSubscription().then((sub) => {
        if (!sub) return;
        const subData = getSubData(sub);
        setCurrentSubData(subData);
        setIsSubscribed(true);
        void subscribePush({
          endpoint: sub.endpoint,
          keys: subData.keys,
          shelterIds: loadNotifShelters(),
        }).catch(() => {});
      });
    });
  }, [isSupported]);

  const subscribe = useCallback(
    async (shelterIds: string[]) => {
      if (!isSupported) return;
      const reg = await navigator.serviceWorker.ready;
      const vapidKey = await fetchVapidKey();
      const sub = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(vapidKey),
      });
      const subData = getSubData(sub);
      await subscribePush({ endpoint: sub.endpoint, keys: subData.keys, shelterIds });
      setCurrentSubData(subData);
      setIsSubscribed(true);
      setNotifShelters(shelterIds);
      localStorage.setItem(NOTIF_SHELTER_KEY, JSON.stringify(shelterIds));
    },
    [isSupported],
  );

  const unsubscribe = useCallback(async () => {
    if (!isSupported) return;
    const reg = await navigator.serviceWorker.ready;
    const sub = await reg.pushManager.getSubscription();
    if (!sub) return;
    const subData = getSubData(sub);
    await unsubscribePush({ endpoint: sub.endpoint, keys: subData.keys });
    await sub.unsubscribe();
    setCurrentSubData(null);
    setIsSubscribed(false);
  }, [isSupported]);

  const updateShelters = useCallback(
    async (shelterIds: string[]) => {
      if (!currentSubData) return;
      setNotifShelters(shelterIds);
      localStorage.setItem(NOTIF_SHELTER_KEY, JSON.stringify(shelterIds));
      await subscribePush({
        endpoint: currentSubData.endpoint,
        keys: currentSubData.keys,
        shelterIds,
      }).catch(() => {});
    },
    [currentSubData],
  );

  return {
    isSubscribed,
    isSupported,
    isIos,
    isStandalone,
    notifShelters,
    currentSubData,
    subscribe,
    unsubscribe,
    updateShelters,
  };
}
