import { useState } from 'react';
import { useAppContext } from '../context/AppContext';
import FilterDrawer from './FilterDrawer';

const SORT_OPTIONS = [
  { value: 'age', label: 'Age' },
  { value: 'name', label: 'Name' },
  { value: 'newest', label: 'Newest' },
];

export default function FilterBar() {
  const { sort, setSort, activeShelters, shelters } = useAppContext();
  const [drawerOpen, setDrawerOpen] = useState(false);

  const badgeCount =
    activeShelters.length === 0 ? 0 : Math.max(0, shelters.length - activeShelters.length);

  return (
    <>
      <div className="filter-bar">
        <div className="filter-group">
          <span className="filter-label">Sort</span>
          <div className="filter-options">
            {SORT_OPTIONS.map(({ value, label }) => (
              <button
                key={value}
                className={`filter-btn${sort === value ? ' active' : ''}`}
                onClick={() => setSort(value)}
              >
                {label}
              </button>
            ))}
          </div>
        </div>
        <button
          className={`filter-trigger-btn${badgeCount > 0 ? ' active' : ''}`}
          onClick={() => setDrawerOpen(true)}
        >
          Filters
          {badgeCount > 0 && <span className="filter-trigger-badge">{badgeCount}</span>}
        </button>
      </div>
      <FilterDrawer open={drawerOpen} onClose={() => setDrawerOpen(false)} />
    </>
  );
}
