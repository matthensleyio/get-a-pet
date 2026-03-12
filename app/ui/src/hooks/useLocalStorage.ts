import { useState } from 'react';

export interface UseLocalStorageResult<T> {
  value: T;
  setValue: (value: T) => void;
}

export function useLocalStorage<T>(key: string, defaultValue: T): UseLocalStorageResult<T> {
  const [state, setState] = useState<T>(() => {
    try {
      const saved = localStorage.getItem(key);
      if (saved === null) return defaultValue;
      return JSON.parse(saved) as T;
    } catch {
      return defaultValue;
    }
  });

  const setValue = (value: T) => {
    setState(value);
    try {
      localStorage.setItem(key, JSON.stringify(value));
    } catch {
      // ignore storage errors
    }
  };

  return { value: state, setValue };
}
