import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Header } from '@/components/Header';
import { ToastContainer } from '@/components/ToastContainer';
import { ErrorBoundary } from '@/components/ErrorBoundary';
import { ClassListPage } from '@/pages/ClassListPage';
import { MyApplicationsPage } from '@/pages/MyApplicationsPage';
import { AdminCreateClassPage } from '@/pages/AdminCreateClassPage';
import { NotFoundPage } from '@/pages/NotFoundPage';

export const App: React.FC = () => {
  const { t } = useTranslation();
  const uiUrl =
    typeof window !== 'undefined'
      ? `${window.location.protocol}//${window.location.host}`
      : 'http://localhost:3000';
  const apiUrl =
    typeof window !== 'undefined'
      ? `${window.location.protocol}//${window.location.hostname}:8080`
      : 'http://localhost:8080';

  return (
    <ErrorBoundary>
      <BrowserRouter>
        <div className="min-h-screen flex flex-col bg-slate-50 text-slate-800">
          <Header />

          <main className="flex-1">
            <Routes>
              <Route path="/" element={<ClassListPage />} />
              <Route path="/my-applications" element={<MyApplicationsPage />} />
              <Route path="/admin/classes/new" element={<AdminCreateClassPage />} />
              <Route path="*" element={<NotFoundPage />} />
            </Routes>
          </main>

          <footer className="bg-white border-t border-slate-200 py-6 mt-12 text-center text-xs text-slate-500">
            <div className="max-w-7xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-2">
              <div>{t('app.footer.copyright')}</div>
              <div className="flex flex-wrap items-center gap-3 font-mono text-xs text-slate-400">
                <span>{t('app.footer.ui', { url: uiUrl })}</span>
                <span>•</span>
                <span>{t('app.footer.api', { url: apiUrl })}</span>
                <span>•</span>
                <span>{t('app.footer.timezone')}</span>
              </div>
            </div>
          </footer>

          <ToastContainer />
        </div>
      </BrowserRouter>
    </ErrorBoundary>
  );
};

export default App;
