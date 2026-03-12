import { useEffect, useRef, useState } from 'react';
import { useAppContext } from '../context/AppContext';
import { IOS_BANNER_DISMISSED_KEY } from '../config/constants';

export default function IosBanner() {
  const { push } = useAppContext();
  const [visible, setVisible] = useState(false);
  const [mounted, setMounted] = useState(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const shouldShow =
    push.isIos && !push.isStandalone && !localStorage.getItem(IOS_BANNER_DISMISSED_KEY);

  useEffect(() => {
    if (!shouldShow) return;
    setMounted(true);
    const id = requestAnimationFrame(() => {
      requestAnimationFrame(() => setVisible(true));
    });
    return () => cancelAnimationFrame(id);
  }, [shouldShow]);

  const dismiss = () => {
    setVisible(false);
    localStorage.setItem(IOS_BANNER_DISMISSED_KEY, '1');
    timerRef.current = setTimeout(() => setMounted(false), 500);
  };

  useEffect(() => {
    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, []);

  if (!mounted) return null;

  return (
    <div className={`ios-install-banner${visible ? ' visible' : ''}`}>
      <div className="ios-banner-inner">
        <div className="ios-banner-text">
          <strong>Get notified about new dogs</strong>
          <span>Add to Home Screen to enable push alerts</span>
        </div>
        <button className="ios-banner-dismiss" aria-label="Dismiss" onClick={dismiss}>
          <svg
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
          >
            <line x1="18" y1="6" x2="6" y2="18" />
            <line x1="6" y1="6" x2="18" y2="18" />
          </svg>
        </button>
      </div>
      <div className="ios-banner-hint">
        Tap the
        <span className="ios-share-icon" aria-label="Share icon">
          <svg
            viewBox="0 0 20 22"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <line x1="10" y1="14" x2="10" y2="1" />
            <polyline points="6 5 10 1 14 5" />
            <path d="M3 13v6a1 1 0 001 1h12a1 1 0 001-1v-6" />
          </svg>
        </span>
        Share button, then <strong>&ldquo;Add to Home Screen&rdquo;</strong>
      </div>
    </div>
  );
}
