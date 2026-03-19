import { useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { fetchStatus } from '../utils/api';
import { timeAgo } from '../utils/timeAgo';
import ShareButton from '../components/ShareButton';
import { useAppContext } from '../context/AppContext';
import type { CachedStatusData } from '../types/api';
import type { DogDto, AdoptedDogDto } from '../types/api';

type AnyDog = DogDto | AdoptedDogDto;

export default function DogDetailPage() {
  const { aid } = useParams<{ aid: string }>();
  const navigate = useNavigate();
  const goBack = () => (window.history.state?.idx > 0 ? navigate(-1) : navigate('/'));
  const queryClient = useQueryClient();
  const { isFavorite, toggleFavorite, shelters } = useAppContext();
  const shelterName = (shelterId: string) =>
    shelters.find((s) => s.shelterId === shelterId)?.shelterName ?? shelterId;

  const stageRef = useRef<HTMLDivElement>(null);
  const imgRef = useRef<HTMLImageElement>(null);

  const syncImgHeight = () => {
    if (imgRef.current && stageRef.current) {
      const h = imgRef.current.getBoundingClientRect().height;
      stageRef.current.style.setProperty('--img-h', `${h}px`);
      stageRef.current.parentElement?.style.setProperty('--img-h', `${h}px`);
    }
  };

  useEffect(() => {
    window.scrollTo(0, 0);
  }, [aid]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') goBack();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [goBack]);

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
    if (dog.shelterId) details.push({ label: 'Shelter', value: shelterName(dog.shelterId) });
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

  const shareUrl = `${window.location.origin}/api/share/${aid}?utm_content=share-link`;

  const valueWidths = ['55%', '15%', '22%', '48%', '18%', '20%'];

  if (!dog && (!fallbackData || new URLSearchParams(window.location.search).has('skeleton'))) {
    return (
      <div className="detail-page">
        <div className="detail-sticky-bar">
          <div className="detail-skeleton-back-btn skeleton-line" />
          <div className="detail-skeleton-fav-btn skeleton-line" />
        </div>
        <div className="detail-hero">
          <div className="detail-skeleton-photo skeleton-img" />
        </div>
        <div className="detail-body">
          <div className="detail-skeleton-name skeleton-line" />
          <div className="detail-rows">
            {valueWidths.map((w, i) => (
              <div key={i} className="detail-row">
                <div className="detail-skeleton-label skeleton-line" />
                <div className="detail-skeleton-value skeleton-line" style={{ width: w }} />
              </div>
            ))}
          </div>
          <div className="detail-actions">
            <div className="detail-skeleton-cta skeleton-line" />
          </div>
        </div>
      </div>
    );
  }

  if (!dog) {
    return (
      <div className="detail-page">
        <div className="detail-sticky-bar">
          <button className="detail-sticky-back" onClick={goBack}>
            <svg
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <polyline points="15 18 9 12 15 6" />
            </svg>
            Back
          </button>
        </div>
        <div className="detail-body">
          <div className="empty-state">
            <p className="empty-title">Dog not found</p>
            <p className="empty-sub">This dog may have been adopted or is no longer available.</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="detail-page">
      <div className="detail-sticky-bar">
        <button className="detail-sticky-back" onClick={goBack}>
          <svg
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <polyline points="15 18 9 12 15 6" />
          </svg>
          Back
        </button>
        <button
          className={`detail-sticky-fav${isFavorite(dog) ? ' active' : ''}`}
          onClick={() => toggleFavorite(dog)}
          aria-label={isFavorite(dog) ? 'Remove from favorites' : 'Add to favorites'}
        >
          <svg
            viewBox="0 0 24 24"
            fill={isFavorite(dog) ? 'currentColor' : 'none'}
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z" />
          </svg>
        </button>
      </div>
      <div className="detail-parallax-stage" ref={stageRef}>
        <div className="detail-hero">
          <img ref={imgRef} src={dog.photoUrl ?? '/dog-placeholder.svg'} alt={dog.name ?? 'Dog'} onLoad={syncImgHeight} />
        </div>
      </div>
      <div className="detail-body">
        <h1 className="detail-name">{dog.name ?? 'Unknown'}</h1>
        {isAdopted && <div className="detail-adopted-pill">Adopted</div>}
        <div className="detail-rows">
          {details.map(({ label, value }) => (
            <div key={label} className="detail-row">
              <span className="detail-label">{label}</span>
              <span>{value}</span>
            </div>
          ))}
        </div>
        <div className="detail-actions">
          {dog.profileUrl && (
            <a
              className="detail-cta-link"
              href={dog.profileUrl}
              target="_blank"
              rel="noopener noreferrer"
            >
              View on {shelterName(dog.shelterId)} Website
            </a>
          )}
          <ShareButton title={dog.name ?? 'Dog'} url={shareUrl} />
        </div>
      </div>
    </div>
  );
}
