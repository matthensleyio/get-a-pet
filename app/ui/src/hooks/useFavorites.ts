import { useLocalStorage } from './useLocalStorage';
import { FAVORITES_KEY } from '../config/constants';

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

export function useFavorites(): FavoritesResult {
  const { value: favorites, setValue: setFavorites } = useLocalStorage<FavoriteKey[]>(
    FAVORITES_KEY,
    [],
  );

  const favoriteKeys = new Set(favorites.map(compositeKey));

  const toggleFavorite = (dog: { aid: string; shelterId: string }) => {
    const key = compositeKey(dog);
    if (favoriteKeys.has(key)) {
      setFavorites(favorites.filter((f) => compositeKey(f) !== key));
    } else {
      setFavorites([...favorites, { aid: dog.aid, shelterId: dog.shelterId }]);
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
