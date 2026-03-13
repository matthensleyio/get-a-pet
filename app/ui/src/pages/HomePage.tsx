import { useEffect, useRef } from 'react';
import { useAppContext } from '../context/AppContext';
import TabBar from '../components/TabBar';
import FilterBar from '../components/FilterBar';
import DogGrid from '../components/DogGrid';
import EmptyState from '../components/EmptyState';

export default function HomePage() {
  const {
    activeTab,
    visibleDogs,
    visibleFavoriteDogs,
    visibleAdoptedDogs,
    statusQuery,
    scrollPosition,
    setScrollPosition,
  } = useAppContext();

  const isRestored = useRef(false);

  useEffect(() => {
    // Restore scroll position only once on mount
    if (!isRestored.current) {
      window.scrollTo(0, scrollPosition);
      isRestored.current = true;
    }

    let timeoutId: number | null = null;
    const handleScroll = () => {
      if (timeoutId) return;
      timeoutId = window.setTimeout(() => {
        setScrollPosition(window.scrollY);
        timeoutId = null;
      }, 150); // Throttle scroll updates
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => {
      window.removeEventListener('scroll', handleScroll);
      if (timeoutId) clearTimeout(timeoutId);
    };
  }, [scrollPosition, setScrollPosition]);

  return (
    <main className="container">
      <TabBar />

      {activeTab === 'available' && (
        <div className="tab-panel">
          <FilterBar />
          {statusQuery.isPending ? (
            <div className="dog-grid grid-loading">
              {Array.from({ length: 12 }).map((_, i) => (
                <div
                  key={i}
                  className="dog-card dog-card--skeleton"
                  style={{ '--i': i } as React.CSSProperties}
                />
              ))}
            </div>
          ) : visibleDogs.length > 0 ? (
            <>
              <DogGrid dogs={visibleDogs} />
            </>
          ) : (
            <EmptyState
              title="No pups available right now"
              subtitle="New dogs arrive often &mdash; check back soon!"
            />
          )}
        </div>
      )}

      {activeTab === 'favorites' && (
        <div className="tab-panel">
          {visibleFavoriteDogs.length > 0 ? (
            <DogGrid dogs={visibleFavoriteDogs} />
          ) : (
            <EmptyState
              title="No favorites yet"
              subtitle="Tap the heart on a dog's card to save them here."
            />
          )}
        </div>
      )}

      {activeTab === 'adopted' && (
        <div className="tab-panel adopted-panel">
          {visibleAdoptedDogs.length > 0 ? (
            <DogGrid dogs={visibleAdoptedDogs} adopted />
          ) : (
            <EmptyState
              title="No recent adoptions"
              subtitle="Dogs adopted from the shelter appear here for 24 hours."
            />
          )}
        </div>
      )}
    </main>
  );
}
