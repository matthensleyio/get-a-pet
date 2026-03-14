export const SHELTER_IDS = ['gpspca', 'kcpp', 'khs'] as const;
export type ShelterId = (typeof SHELTER_IDS)[number];

export const SHELTER_NAMES: Record<string, string> = {
  gpspca: 'Great Plains SPCA',
  kcpp: 'KC Pet Project',
  khs: 'KHS',
};

export const PAGE_SIZE = 20;
export const POLL_INTERVAL = 30000;

export const FAVORITES_KEY = 'fav-pets';
export const SORT_KEY = 'khs-sort';
export const SHELTER_FILTER_KEY = 'shelter-filter';
export const NOTIF_SHELTER_KEY = 'notif-shelters';
export const IOS_BANNER_DISMISSED_KEY = 'ios-banner-dismissed';

export const DB_NAME = 'get-a-pet';
export const DB_VERSION = 2;
export const DB_STORE_NAME = 'status';
