import { useState } from 'react';
import { useAppContext } from '../context/AppContext';

interface FilterDrawerProps {
  open: boolean;
  onClose: () => void;
}

export default function FilterDrawer({ open, onClose }: FilterDrawerProps) {
  const { shelters, activeShelters, setActiveShelters } = useAppContext();
  const [search, setSearch] = useState('');

  if (!open) return null;

  const filtered = shelters.filter((s) =>
    s.shelterName.toLowerCase().includes(search.toLowerCase()),
  );

  const showReset =
    shelters.length > 0 && !shelters.every((s) => activeShelters.includes(s.shelterId));

  const handleToggle = (id: string) => {
    if (activeShelters.includes(id)) {
      setActiveShelters(activeShelters.filter((s) => s !== id));
    } else {
      setActiveShelters([...activeShelters, id]);
    }
  };

  return (
    <>
      <div className="filter-drawer-overlay" onClick={onClose} />
      <div className="filter-drawer">
        <div className="filter-drawer-header">
          <span className="filter-drawer-title">Filters</span>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            {showReset && (
              <button
                className="filter-drawer-reset"
                onClick={() => setActiveShelters(shelters.map((s) => s.shelterId))}
              >
                Reset
              </button>
            )}
            <button className="filter-drawer-close" onClick={onClose}>
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            </button>
          </div>
        </div>
        <div className="filter-drawer-body">
          <span className="filter-drawer-section-title">Shelters</span>
          <input
            className="filter-search"
            type="search"
            placeholder="Search shelters..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <div className="filter-drawer-shortcuts">
            <button
              className="filter-drawer-reset"
              onClick={() => setActiveShelters(shelters.map((s) => s.shelterId))}
            >
              Select All
            </button>
            <button className="filter-drawer-reset" onClick={() => setActiveShelters([])}>
              Clear All
            </button>
          </div>
          <div className="filter-shelter-list">
            {filtered.map(({ shelterId, shelterName }) => (
              <button
                key={shelterId}
                className={`notif-shelter-btn${activeShelters.includes(shelterId) ? ' active' : ''}`}
                onClick={() => handleToggle(shelterId)}
              >
                {shelterName}
              </button>
            ))}
          </div>
        </div>
      </div>
    </>
  );
}
