import { useAppContext } from '../context/AppContext';
import type { ActiveTab } from '../context/AppContext';

export default function TabBar() {
  const { activeTab, setActiveTab, statusQuery, totalCount, favoriteDogs } = useAppContext();
  const data = statusQuery.data;

  const go = (tab: ActiveTab) => setActiveTab(tab);

  return (
    <div className="tab-bar">
      <button
        className={`tab-btn${activeTab === 'available' ? ' active' : ''}`}
        onClick={() => go('available')}
      >
        Available{' '}
        {data && <span className="count-badge">{totalCount}</span>}
      </button>
      {favoriteDogs.length > 0 && (
        <button
          className={`tab-btn${activeTab === 'favorites' ? ' active' : ''}`}
          onClick={() => go('favorites')}
        >
          Favorites{' '}
          <span className="count-badge">{favoriteDogs.length}</span>
        </button>
      )}
      <button
        className={`tab-btn${activeTab === 'adopted' ? ' active' : ''}`}
        onClick={() => go('adopted')}
      >
        Recently Adopted{' '}
        {data && data.recentlyAdopted.length > 0 && (
          <span className="count-badge adopted-count">{data.recentlyAdopted.length}</span>
        )}
      </button>
    </div>
  );
}
