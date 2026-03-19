import { Link } from 'react-router-dom';
import { forwardRef } from 'react';
import { cleanBreed } from '../utils/breed';
import { timeAgo } from '../utils/timeAgo';
import { useAppContext } from '../context/AppContext';
import type { DogDto, AdoptedDogDto } from '../types/api';

interface DogCardProps {
  dog: DogDto | AdoptedDogDto;
  index: number;
  adopted?: boolean;
}

const DogCard = forwardRef<HTMLAnchorElement, DogCardProps>(
  ({ dog, index, adopted = false }, ref) => {
    const { shelters } = useAppContext();
    const breed = cleanBreed(dog.breed);
    const shelterName = shelters.find(s => s.shelterId === dog.shelterId)?.shelterName ?? dog.shelterId;
    const isAdoptedDog = 'adoptedAt' in dog;
    const isNew =
      !isAdoptedDog && Date.now() - new Date(dog.firstSeen).getTime() < 86400000;

    const tags =
      adopted || isAdoptedDog
        ? [dog.size, dog.weight].filter(Boolean)
        : [shelterName, dog.size, dog.weight].filter(Boolean);

    return (
      <Link
        ref={ref}
        to={`/dogs/${dog.aid}/details`}
        className={`dog-card${isAdoptedDog ? ' dog-card--adopted' : ''}`}
        style={{ '--i': Math.min(index, 15) } as React.CSSProperties}
      >
        <img
          src={dog.photoUrl ?? '/dog-placeholder.svg'}
          alt={dog.name ?? 'Dog'}
          loading="lazy"
        />
        {isNew && <span className="new-badge">New</span>}
        {isAdoptedDog && (
          <span className="adopted-badge">
            Adopted {timeAgo((dog as AdoptedDogDto).adoptedAt)}
          </span>
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
  },
);

export default DogCard;
