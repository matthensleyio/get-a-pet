import { Link } from 'react-router-dom';
import { cleanBreed } from '../utils/breed';
import { timeAgo } from '../utils/timeAgo';
import { SHELTER_NAMES } from '../config/constants';
import type { DogDto, AdoptedDogDto } from '../types/api';

interface DogCardProps {
  dog: DogDto | AdoptedDogDto;
  index: number;
  adopted?: boolean;
}

export default function DogCard({ dog, index, adopted = false }: DogCardProps) {
  const breed = cleanBreed(dog.breed);
  const shelterName = SHELTER_NAMES[dog.shelterId] ?? dog.shelterId;
  const isNew =
    !adopted && Date.now() - new Date(dog.firstSeen).getTime() < 86400000;

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
