import { useState, useRef, forwardRef } from 'react';

interface PhotoCarouselProps {
  photos: string[];
  alt: string;
  onLoad?: () => void;
}

const PhotoCarousel = forwardRef<HTMLImageElement, PhotoCarouselProps>(
  function PhotoCarousel({ photos, alt, onLoad }, ref) {
    const [index, setIndex] = useState(0);
    const startX = useRef(0);
    const startY = useRef(0);
    const isDragging = useRef(false);
    const didSwipe = useRef(false);

    if (photos.length <= 1) {
      return (
        <img
          ref={ref}
          src={photos[0] ?? '/dog-placeholder.svg'}
          alt={alt}
          onLoad={onLoad}
          draggable={false}
        />
      );
    }

    const handlePointerDown = (e: React.PointerEvent) => {
      startX.current = e.clientX;
      startY.current = e.clientY;
      isDragging.current = true;
      didSwipe.current = false;
    };

    const handlePointerUp = (e: React.PointerEvent) => {
      if (!isDragging.current) return;
      isDragging.current = false;
      const dx = e.clientX - startX.current;
      const dy = e.clientY - startY.current;
      if (Math.abs(dx) > Math.abs(dy) && Math.abs(dx) > 36) {
        didSwipe.current = true;
        if (dx < 0 && index < photos.length - 1) setIndex((i) => i + 1);
        else if (dx > 0 && index > 0) setIndex((i) => i - 1);
      }
    };

    const handlePointerLeave = () => {
      isDragging.current = false;
    };

    return (
      <div
        className="photo-carousel"
        onPointerDown={handlePointerDown}
        onPointerUp={handlePointerUp}
        onPointerLeave={handlePointerLeave}
      >
        <div
          className="photo-carousel-track"
          style={{ transform: `translateX(${-index * 100}%)` }}
        >
          {photos.map((url, i) => (
            <img
              key={i}
              ref={i === 0 ? ref : undefined}
              src={url}
              alt={alt}
              onLoad={i === 0 ? onLoad : undefined}
              draggable={false}
            />
          ))}
        </div>
        <div className="photo-carousel-dots" aria-hidden="true">
          {photos.map((_url, i) => (
            <span
              key={i}
              className={`photo-carousel-dot${i === index ? ' active' : ''}`}
            />
          ))}
        </div>
      </div>
    );
  }
);

export default PhotoCarousel;
