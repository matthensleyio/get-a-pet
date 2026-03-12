import { useState } from 'react';
import { useAppContext } from '../context/AppContext';
import { SHELTER_IDS, SHELTER_NAMES } from '../config/constants';

export default function NotificationSetupModal() {
  const { push, activeShelters, setIsNotifSetupOpen } = useAppContext();
  const [selected, setSelected] = useState<string[]>(
    activeShelters.length > 0 ? activeShelters : [...SHELTER_IDS],
  );

  const toggleShelter = (id: string) => {
    setSelected((prev) =>
      prev.includes(id) ? prev.filter((s) => s !== id) : [...prev, id],
    );
  };

  const handleConfirm = () => {
    if (selected.length === 0) return;
    void push.subscribe(selected).then(() => setIsNotifSetupOpen(false));
  };

  const handleOverlayClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (e.target === e.currentTarget) setIsNotifSetupOpen(false);
  };

  return (
    <div className="notif-setup-overlay active" onClick={handleOverlayClick}>
      <div
        className="notif-setup-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="notif-setup-title"
      >
        <div className="notif-setup-header">
          <div className="notif-setup-icon-wrap">
            <svg
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2.2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
              <path d="M13.73 21a2 2 0 0 1-3.46 0" />
            </svg>
          </div>
          <h2 id="notif-setup-title">Get Alerts</h2>
          <p>Choose which shelters to follow</p>
        </div>
        <div className="notif-setup-shelters">
          {SHELTER_IDS.map((id) => (
            <label key={id} className="notif-setup-row">
              <input
                type="checkbox"
                className="notif-setup-check"
                value={id}
                checked={selected.includes(id)}
                onChange={() => toggleShelter(id)}
              />
              <span className="notif-setup-box" />
              <span>{SHELTER_NAMES[id]}</span>
            </label>
          ))}
        </div>
        <div className="notif-setup-actions">
          <button
            className="notif-setup-confirm-btn"
            disabled={selected.length === 0}
            onClick={handleConfirm}
          >
            Turn On Alerts
          </button>
          <button className="notif-setup-cancel-btn" onClick={() => setIsNotifSetupOpen(false)}>
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
}
