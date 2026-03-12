import { useEffect } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AppProvider, useAppContext } from './context/AppContext';
import { useOnlineStatus } from './hooks/useOnlineStatus';
import Header from './components/Header';
import OfflineBanner from './components/OfflineBanner';
import Footer from './components/Footer';
import IosBanner from './components/IosBanner';
import NotificationSetupModal from './components/NotificationSetupModal';
import HomePage from './pages/HomePage';
import DogDetailPage from './pages/DogDetailPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
});

function AppShell() {
  const { setIsNotifPanelOpen, setIsNotifSetupOpen, isNotifSetupOpen } = useAppContext();
  const { isOnline } = useOnlineStatus();

  useEffect(() => {
    document.body.classList.toggle('offline', !isOnline);
  }, [isOnline]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setIsNotifPanelOpen(false);
        setIsNotifSetupOpen(false);
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [setIsNotifPanelOpen, setIsNotifSetupOpen]);

  useEffect(() => {
    if (!('serviceWorker' in navigator)) return;
    const prevController = navigator.serviceWorker.controller;
    void navigator.serviceWorker.register('/sw.js');
    const handleControllerChange = () => {
      if (prevController) location.reload();
    };
    navigator.serviceWorker.addEventListener('controllerchange', handleControllerChange);
    return () =>
      navigator.serviceWorker.removeEventListener('controllerchange', handleControllerChange);
  }, []);

  return (
    <div id="content-wrapper">
      <div className="page-top">
        <Header />
        <div className="header-stripe" />
        <OfflineBanner />
      </div>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/dogs/:aid/details" element={<DogDetailPage />} />
      </Routes>
      <Footer />
      {isNotifSetupOpen && <NotificationSetupModal />}
      <IosBanner />
    </div>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AppProvider>
          <AppShell />
        </AppProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
