import { openDB } from 'idb';
import type { StatusResponseDto } from '../types/api';
import { DB_NAME, DB_VERSION, DB_STORE_NAME } from '../config/constants';

async function getDb() {
  return openDB(DB_NAME, DB_VERSION, {
    upgrade(db) {
      if (!db.objectStoreNames.contains(DB_STORE_NAME)) {
        db.createObjectStore(DB_STORE_NAME);
      }
    },
  });
}

export async function saveToDb(data: StatusResponseDto): Promise<void> {
  const db = await getDb();
  await db.put(DB_STORE_NAME, data, 'latest');
}

export async function loadFromDb(): Promise<StatusResponseDto | null> {
  const db = await getDb();
  return ((await db.get(DB_STORE_NAME, 'latest')) as StatusResponseDto | undefined) ?? null;
}
