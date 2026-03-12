import { useAppContext } from '../context/AppContext';

export default function NotificationBell() {
  const { push, isNotifPanelOpen, setIsNotifPanelOpen, setIsNotifSetupOpen } = useAppContext();

  const handleClick = () => {
    if (!push.isSupported) {
      if (push.isIos && !push.isStandalone) {
        setIsNotifSetupOpen(true);
      }
      return;
    }
    if (push.isSubscribed) {
      setIsNotifPanelOpen(!isNotifPanelOpen);
    } else {
      setIsNotifSetupOpen(true);
    }
  };

  return (
    <button
      className={`subscribe-btn${push.isSubscribed ? ' subscribed' : ''}`}
      aria-label="Alert settings"
      onClick={handleClick}
    >
      <svg
        className="bell-icon"
        viewBox="0 0 24 24"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
        <path d="M13.73 21a2 2 0 0 1-3.46 0" />
      </svg>
    </button>
  );
}
