import { Link } from 'react-router-dom';
import { forwardRef } from 'react';
import { cleanBreed } from '../utils/breed';
import { timeAgo } from '../utils/timeAgo';
import type { DogDto, AdoptedDogDto } from '../types/api';

interface DogCardProps {
  dog: DogDto | AdoptedDogDto;
  index: number;
}

const DogCard = forwardRef<HTMLAnchorElement, DogCardProps>(
  ({ dog, index }, ref) => {
    const breed = cleanBreed(dog.breed);
    const isAdoptedDog = 'adoptedAt' in dog;
    const isNew =
      !isAdoptedDog && Date.now() - new Date(dog.firstSeen).getTime() < 86400000;

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
        {isAdoptedDog && (
          <span className="adopted-badge">
            Adopted {timeAgo((dog as AdoptedDogDto).adoptedAt)}
          </span>
        )}
        <div className="dog-card-info">
          <div className="dog-card-name-row">
            <h3>{dog.name ?? 'Unknown'}</h3>
            {isNew && <span className="new-badge">New</span>}
          </div>
          {(dog.gender || dog.age) && (
            <p className="dog-card-meta">{[dog.gender, dog.age].filter(Boolean).join(' \u00b7 ')}</p>
          )}
          {breed && <p className="dog-card-breed">{breed}</p>}
          {(dog.color || dog.weight) && (
            <p className="dog-card-meta">{[dog.color, dog.weight].filter(Boolean).join(' \u00b7 ')}</p>
          )}
        </div>
      </Link>
    );
  },
);

export default DogCard;
