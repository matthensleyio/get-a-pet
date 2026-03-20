import { useEffect, useRef } from 'react';
import { useLocalStorage } from './useLocalStorage';
import { FAVORITES_KEY } from '../config/constants';
import { fetchFavorites, addFavorite, removeFavorite } from '../utils/api';

interface FavoriteKey {
  aid: string;
  shelterId: string;
}

export interface FavoritesResult {
  favoriteKeys: Set<string>;
  toggleFavorite: (dog: { aid: string; shelterId: string }) => void;
  isFavorite: (dog: { aid: string; shelterId: string }) => boolean;
  pruneToKeys: (validCompositeKeys: Set<string>) => void;
}

export function compositeKey(dog: { aid: string; shelterId: string }): string {
  return `${dog.shelterId}:${dog.aid}`;
}

export function useFavorites(subscriptionEndpoint: string | null): FavoritesResult {
  const { value: favorites, setValue: setFavorites } = useLocalStorage<FavoriteKey[]>(
    FAVORITES_KEY,
    [],
  );

  const favoritesRef = useRef(favorites);
  useEffect(() => {
    favoritesRef.current = favorites;
  }, [favorites]);

  useEffect(() => {
    if (!subscriptionEndpoint) return;

    fetchFavorites(subscriptionEndpoint)
      .then((serverFavorites) => {
        const serverKeys = new Set(serverFavorites.map(compositeKey));
        const localFavorites = favoritesRef.current;

        const merged = [...serverFavorites];
        for (const fav of localFavorites) {
          if (!serverKeys.has(compositeKey(fav))) {
            merged.push(fav);
            addFavorite(subscriptionEndpoint, fav.aid, fav.shelterId).catch(() => {});
          }
        }

        setFavorites(merged);
      })
      .catch(() => {});
  }, [subscriptionEndpoint]); // eslint-disable-line react-hooks/exhaustive-deps

  const favoriteKeys = new Set(favorites.map(compositeKey));

  const toggleFavorite = (dog: { aid: string; shelterId: string }) => {
    const key = compositeKey(dog);
    if (favoriteKeys.has(key)) {
      setFavorites(favorites.filter((f) => compositeKey(f) !== key));
      if (subscriptionEndpoint) {
        removeFavorite(subscriptionEndpoint, dog.aid, dog.shelterId).catch(() => {});
      }
    } else {
      setFavorites([...favorites, { aid: dog.aid, shelterId: dog.shelterId }]);
      if (subscriptionEndpoint) {
        addFavorite(subscriptionEndpoint, dog.aid, dog.shelterId).catch(() => {});
      }
    }
  };

  const isFavorite = (dog: { aid: string; shelterId: string }) =>
    favoriteKeys.has(compositeKey(dog));

  const pruneToKeys = (validCompositeKeys: Set<string>) => {
    const pruned = favorites.filter((f) => validCompositeKeys.has(compositeKey(f)));
    if (pruned.length !== favorites.length) {
      setFavorites(pruned);
    }
  };

  return { favoriteKeys, toggleFavorite, isFavorite, pruneToKeys };
}
