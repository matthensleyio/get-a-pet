import { useAppContext } from '../context/AppContext';
import { timeAgo } from '../utils/timeAgo';

export default function Footer() {
  const { statusQuery, monitorError, forceMonitor } = useAppContext();
  const data = statusQuery.data;

  const isActive = data?.isMonitoringActive ?? true;
  const isPaused = !isActive;
  const hasError = !!monitorError;

  const handlePillClick = () => {
    if (!isPaused) return;
    forceMonitor();
  };

  return (
    <footer className="site-footer">
      <div className="footer-inner">
        <div
          className={`status-pill${isPaused ? ' status-pill--paused' : ''}`}
          title={isPaused ? 'Click to force a check' : ''}
          onClick={handlePillClick}
          style={isPaused ? { cursor: 'pointer' } : undefined}
        >
          <span
            className={`status-indicator${!isActive ? ' inactive' : ''}${hasError ? ' error' : ''}`}
          />
          <span>
            {hasError ? 'Monitor Error' : isPaused ? 'Paused (after hours)' : 'Monitoring'}
          </span>
        </div>
        {data?.lastChecked && (
          <span className="last-checked">Updated {timeAgo(data.lastChecked)}</span>
        )}
      </div>
    </footer>
  );
}
