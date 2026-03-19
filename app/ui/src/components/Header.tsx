import { useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import { useAppContext } from '../context/AppContext';
import NotificationBell from './NotificationBell';
import NotificationPanel from './NotificationPanel';

export default function Header() {
  const { isNotifPanelOpen, setIsNotifPanelOpen } = useAppContext();
  const groupRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClick = (e: MouseEvent) => {
      if (
        groupRef.current &&
        !groupRef.current.contains(e.target as Node) &&
        isNotifPanelOpen
      ) {
        setIsNotifPanelOpen(false);
      }
    };
    document.addEventListener('click', handleClick);
    return () => document.removeEventListener('click', handleClick);
  }, [isNotifPanelOpen, setIsNotifPanelOpen]);

  return (
    <header className="site-header">
      <div className="header-inner">
        <div className="header-brand">
          <Link to="/" className="site-title">Get-A-Pet</Link>
          <p className="header-tagline">Be the first to know when your perfect pet is available.</p>
        </div>
        <div className="header-controls">
          <div className="notif-group" ref={groupRef}>
            <NotificationBell />
            {isNotifPanelOpen && <NotificationPanel />}
          </div>
        </div>
      </div>
    </header>
  );
}
