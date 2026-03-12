import { Link } from 'react-router-dom';
import { cleanBreed } from '../utils/breed';
import { timeAgo } from '../utils/timeAgo';
import { SHELTER_NAMES } from '../config/constants';
import { useAppContext } from '../context/AppContext';
import type { DogDto, AdoptedDogDto } from '../types/api';

interface DogCardProps {
  dog: DogDto | AdoptedDogDto;
  index: number;
  adopted?: boolean;
}

export default function DogCard({ dog, index, adopted = false }: DogCardProps) {
  const { isFavorite, toggleFavorite } = useAppContext();
  const breed = cleanBreed(dog.breed);
  const shelterName = SHELTER_NAMES[dog.shelterId] ?? dog.shelterId;
  const isNew =
    !adopted && Date.now() - new Date(dog.firstSeen).getTime() < 86400000;
  const faved = isFavorite(dog);

  const tags = adopted
    ? [dog.size, dog.weight].filter(Boolean)
    : [shelterName, dog.size, dog.weight].filter(Boolean);

  return (
    <Link
      to={`/dogs/${dog.aid}/details`}
      className="dog-card"
      style={{ '--i': Math.min(index, 15) } as React.CSSProperties}
    >
      {dog.photoUrl ? (
        <img src={dog.photoUrl} alt={dog.name ?? 'Dog'} loading="lazy" />
      ) : (
        <img src="" alt="No photo" style={{ background: 'var(--surface)' }} />
      )}
      {isNew && <span className="new-badge">New</span>}
      {adopted && 'adoptedAt' in dog && (
        <span className="adopted-badge">Adopted {timeAgo(dog.adoptedAt)}</span>
      )}
      <button
        className={`dog-card-fav${faved ? ' active' : ''}`}
        aria-label={faved ? 'Remove from favorites' : 'Add to favorites'}
        onClick={(e) => {
          e.preventDefault();
          e.stopPropagation();
          toggleFavorite(dog);
        }}
      >
        <svg viewBox="0 0 24 24" fill={faved ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z" />
        </svg>
      </button>
      <div className="dog-card-info">
        <h3>{dog.name ?? 'Unknown'}</h3>
        <p>{[dog.gender, dog.age].filter(Boolean).join(' \u00b7 ')}</p>
        {breed && <p className="dog-card-breed">{breed}</p>}
        {tags.length > 0 && (
          <div className="dog-card-tags">
            {tags.map((tag) => (
              <span key={tag} className="dog-card-tag">
                {tag}
              </span>
            ))}
          </div>
        )}
      </div>
    </Link>
  );
}
