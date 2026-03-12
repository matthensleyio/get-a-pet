import { useAppContext } from '../context/AppContext';
import { SHELTER_IDS, SHELTER_NAMES } from '../config/constants';

const SORT_OPTIONS = [
  { value: 'age', label: 'Age' },
  { value: 'name', label: 'Name' },
  { value: 'newest', label: 'Newest' },
];

export default function FilterBar() {
  const { sort, setSort, activeShelters, setActiveShelters } = useAppContext();

  const toggleShelter = (id: string) => {
    if (activeShelters.includes(id)) {
      if (activeShelters.length > 1) {
        setActiveShelters(activeShelters.filter((s) => s !== id));
      }
    } else {
      setActiveShelters([...activeShelters, id]);
    }
  };

  return (
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
      <div className="filter-group">
        <span className="filter-label">Shelter</span>
        <div className="filter-options">
          {SHELTER_IDS.map((id) => (
            <button
              key={id}
              className={`filter-btn${activeShelters.includes(id) ? ' active' : ''}`}
              onClick={() => toggleShelter(id)}
            >
              {SHELTER_NAMES[id]}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}
