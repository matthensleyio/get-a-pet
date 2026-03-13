import { useEffect, useRef } from 'react';
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
          <h1 className="site-title">Get a Pet</h1>
          <p className="header-tagline">The fastest way from shelter to sofa.</p>
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
