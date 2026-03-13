import { useCallback, useRef } from 'react';
import DogCard from './DogCard';
import { useAppContext } from '../context/AppContext';
import { PAGE_SIZE } from '../config/constants';
import type { DogDto, AdoptedDogDto } from '../types/api';

interface DogGridProps {
  dogs: (DogDto | AdoptedDogDto)[];
  adopted?: boolean;
}

export default function DogGrid({ dogs, adopted = false }: DogGridProps) {
  const { setPage, page } = useAppContext();
  const observer = useRef<IntersectionObserver | null>(null);

  const lastDogRef = useCallback(
    (node: HTMLAnchorElement) => {
      if (observer.current) observer.current.disconnect();
      observer.current = new IntersectionObserver((entries) => {
        if (entries[0].isIntersecting) {
          setPage((prevPage: number) => prevPage + 1);
        }
      });
      if (node) observer.current.observe(node);
    },
    [setPage],
  );

  return (
    <div className="dog-grid">
      {dogs.map((dog, index) => {
        // Sentinel is the start of the last loaded page (page * PAGE_SIZE - PAGE_SIZE)
        // We want to load the NEXT page when we reach the start of the CURRENT last page
        // so that we always have one extra page loaded.
        const sentinelIndex = (page - 1) * PAGE_SIZE;
        const isSentinel = index === sentinelIndex && index > 0;

        return (
          <DogCard
            key={`${dog.shelterId}:${dog.aid}`}
            ref={isSentinel ? lastDogRef : undefined}
            dog={dog}
            index={index}
            adopted={adopted}
          />
        );
      })}
    </div>
  );
}
