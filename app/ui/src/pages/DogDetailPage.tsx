import { useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { fetchStatus } from '../utils/api';
import { timeAgo } from '../utils/timeAgo';
import { SHELTER_NAMES } from '../config/constants';
import ShareButton from '../components/ShareButton';
import { useAppContext } from '../context/AppContext';
import type { CachedStatusData } from '../types/api';
import type { DogDto, AdoptedDogDto } from '../types/api';

type AnyDog = DogDto | AdoptedDogDto;

export default function DogDetailPage() {
  const { aid } = useParams<{ aid: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { isFavorite, toggleFavorite } = useAppContext();

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') navigate(-1);
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [navigate]);

  const allCached = queryClient.getQueriesData<CachedStatusData>({ queryKey: ['status'] });
  let cachedDog: AnyDog | undefined;
  for (const [, data] of allCached) {
    if (!data) continue;
    cachedDog =
      data.dogs.find((d) => d.aid === aid) ??
      data.recentlyAdopted.find((d) => d.aid === aid);
    if (cachedDog) break;
  }

  const { data: fallbackData } = useQuery({
    queryKey: ['status'] as const,
    queryFn: async () => {
      const raw = await fetchStatus();
      if ('offline' in raw) return null;
      return raw;
    },
    enabled: !cachedDog,
    staleTime: 60000,
    retry: false,
  });

  const dog: AnyDog | undefined =
    cachedDog ??
    fallbackData?.dogs.find((d) => d.aid === aid) ??
    fallbackData?.recentlyAdopted.find((d) => d.aid === aid);

  const isAdopted = dog && 'adoptedAt' in dog;
  const shelteredDate = dog ? (dog.intakeDate ?? (dog as DogDto).listingDate ?? null) : null;

  const details: { label: string; value: string }[] = [];
  if (dog) {
    if (dog.shelterId) details.push({ label: 'Shelter', value: SHELTER_NAMES[dog.shelterId] ?? dog.shelterId });
    if (dog.gender) details.push({ label: 'Gender', value: dog.gender });
    if (dog.age) details.push({ label: 'Age', value: dog.age });
    if (dog.breed) details.push({ label: 'Breed', value: dog.breed });
    if (dog.color) details.push({ label: 'Color', value: dog.color });
    if (dog.size) details.push({ label: 'Size', value: dog.size });
    if (dog.weight) details.push({ label: 'Weight', value: dog.weight });
    if (shelteredDate)
      details.push({
        label: 'At shelter since',
        value: new Date(shelteredDate).toLocaleDateString(undefined, {
          year: 'numeric',
          month: 'short',
          day: 'numeric',
        }),
      });
    if (isAdopted && 'adoptedAt' in dog)
      details.push({ label: 'Adopted', value: timeAgo(dog.adoptedAt) });
  }

  const detailUrl = `${window.location.origin}/dogs/${aid}/details`;

  if (!dog && !fallbackData) {
    return (
      <main className="container">
        <div className="dog-detail-page">
          <div className="dog-detail-loading">Loading&hellip;</div>
        </div>
      </main>
    );
  }

  if (!dog) {
    return (
      <main className="container">
        <div className="dog-detail-page">
          <Link to="/" className="dog-detail-back">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="15 18 9 12 15 6" />
            </svg>
            Back
          </Link>
          <div className="empty-state">
            <p className="empty-title">Dog not found</p>
            <p className="empty-sub">This dog may have been adopted or is no longer available.</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="container">
      <div className="dog-detail-page">
        <Link to="/" className="dog-detail-back">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          Back
        </Link>
        <div className="dog-detail-content">
          <div className="modal-image-wrapper">
            {dog.photoUrl && <img src={dog.photoUrl} alt={dog.name ?? 'Dog'} />}
            <button
              className={`dog-detail-fav-overlay${isFavorite(dog) ? ' active' : ''}`}
              onClick={() => toggleFavorite(dog)}
              aria-label={isFavorite(dog) ? 'Remove from favorites' : 'Add to favorites'}
            >
              <svg viewBox="0 0 24 24" fill={isFavorite(dog) ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z" />
              </svg>
            </button>
          </div>
          <div className="modal-body">
            <h2>{dog.name ?? 'Unknown'}</h2>
            <div className="modal-details">
              {details.map(({ label, value }) => (
                <div key={label} className="modal-detail">
                  <span className="label">{label}</span>
                  <span>{value}</span>
                </div>
              ))}
            </div>
            <div className="dog-detail-actions">
              {dog.profileUrl && (
                <a
                  className="modal-link"
                  href={dog.profileUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  View on {SHELTER_NAMES[dog.shelterId] ?? 'Shelter'} Website
                </a>
              )}
              <ShareButton title={dog.name ?? 'Dog'} url={detailUrl} />
            </div>
          </div>
        </div>
      </div>
    </main>
  );
}
