import { useState } from 'react';
import { useAppContext } from '../context/AppContext';

export default function NotificationPanel() {
  const { push, setIsNotifPanelOpen, shelters } = useAppContext();
  const [confirming, setConfirming] = useState(false);
  const [search, setSearch] = useState('');

  const handleShelterToggle = (shelterId: string) => {
    const current = push.notifShelters;
    if (current.includes(shelterId)) {
      if (current.length > 1) {
        void push.updateShelters(current.filter((s) => s !== shelterId));
      }
    } else {
      void push.updateShelters([...current, shelterId]);
    }
  };

  const handleUnsubscribeConfirm = () => {
    void push.unsubscribe().then(() => {
      setIsNotifPanelOpen(false);
      setConfirming(false);
    });
  };

  return (
    <div className="notif-panel">
      {!confirming ? (
        <div id="notif-panel-main">
          <div className="notif-shelter-filter">
            <span className="notif-section-title">Notify me about</span>
            <input
              className="filter-search"
              type="search"
              placeholder="Search shelters..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <div className="notif-shelter-options">
              {shelters.filter((s) => s.shelterName.toLowerCase().includes(search.toLowerCase())).map(({ shelterId, shelterName }) => (
                <button
                  key={shelterId}
                  className={`notif-shelter-btn${push.notifShelters.includes(shelterId) ? ' active' : ''}`}
                  onClick={() => handleShelterToggle(shelterId)}
                >
                  {shelterName}
                </button>
              ))}
            </div>
          </div>
          <button className="unsubscribe-btn" onClick={() => setConfirming(true)}>
            Turn Off Alerts
          </button>
        </div>
      ) : (
        <div className="notif-panel-confirm">
          <p className="notif-confirm-text">Stop getting alerts?</p>
          <div className="notif-confirm-actions">
            <button className="unsubscribe-confirm-btn" onClick={handleUnsubscribeConfirm}>
              Yes, turn off
            </button>
            <button className="unsubscribe-cancel-btn" onClick={() => setConfirming(false)}>
              Keep alerts
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
