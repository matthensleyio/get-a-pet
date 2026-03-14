import { useAppContext } from '../context/AppContext';
import { timeAgo } from '../utils/timeAgo';

export default function Footer() {
  const { statusQuery } = useAppContext();
  const data = statusQuery.data;

  const isActive = data?.isMonitoringActive ?? true;
  const isPaused = !isActive;

  return (
    <footer className="site-footer">
      <div className="footer-inner">
        <div className={`status-pill${isPaused ? ' status-pill--paused' : ''}`}>
          <span className={`status-indicator${!isActive ? ' inactive' : ''}`} />
          <span>{isPaused ? 'Paused (after hours)' : 'Monitoring'}</span>
        </div>
        {data?.lastChecked && (
          <span className="last-checked">Updated {timeAgo(data.lastChecked)}</span>
        )}
      </div>
    </footer>
  );
}
