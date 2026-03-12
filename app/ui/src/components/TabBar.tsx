import { useAppContext } from '../context/AppContext';

export default function TabBar() {
  const { activeTab, setActiveTab, statusQuery } = useAppContext();
  const data = statusQuery.data;

  return (
    <div className="tab-bar">
      <button
        className={`tab-btn${activeTab === 'available' ? ' active' : ''}`}
        onClick={() => setActiveTab('available')}
      >
        Available{' '}
        {data && (
          <span className="count-badge">{data.totalCount}</span>
        )}
      </button>
      <button
        className={`tab-btn${activeTab === 'adopted' ? ' active' : ''}`}
        onClick={() => setActiveTab('adopted')}
      >
        Recently Adopted{' '}
        {data && data.recentlyAdopted.length > 0 && (
          <span className="count-badge adopted-count">{data.recentlyAdopted.length}</span>
        )}
      </button>
    </div>
  );
}
