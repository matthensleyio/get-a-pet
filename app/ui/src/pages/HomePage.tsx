import { useAppContext } from '../context/AppContext';
import TabBar from '../components/TabBar';
import FilterBar from '../components/FilterBar';
import DogGrid from '../components/DogGrid';
import Pagination from '../components/Pagination';
import EmptyState from '../components/EmptyState';
import type { AdoptedDogDto } from '../types/api';

export default function HomePage() {
  const { activeTab, visibleDogs, favoriteDogs, statusQuery } = useAppContext();
  const data = statusQuery.data;

  const adopted = data
    ? [...data.recentlyAdopted].sort(
        (a: AdoptedDogDto, b: AdoptedDogDto) =>
          new Date(b.adoptedAt).getTime() - new Date(a.adoptedAt).getTime(),
      )
    : [];

  return (
    <main className="container">
      <TabBar />

      {activeTab === 'available' && (
        <div className="tab-panel">
          <FilterBar />
          {visibleDogs.length > 0 ? (
            <>
              <DogGrid dogs={visibleDogs} />
              <Pagination />
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
          {favoriteDogs.length > 0 ? (
            <DogGrid dogs={favoriteDogs} />
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
          {adopted.length > 0 ? (
            <DogGrid dogs={adopted} adopted />
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
