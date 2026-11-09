import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Header } from '@/components/Header';
import { ToastContainer } from '@/components/ToastContainer';
import { ErrorBoundary } from '@/components/ErrorBoundary';
import { ClassListPage } from '@/pages/ClassListPage';
import { MyApplicationsPage } from '@/pages/MyApplicationsPage';
import { AdminCreateClassPage } from '@/pages/AdminCreateClassPage';
import { NotFoundPage } from '@/pages/NotFoundPage';

export const App: React.FC = () => {
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
              <div>
                © 2026 স্কুল ভর্তি পোর্টাল (School Admission Portal) — গণপ্রজাতন্ত্রী বাংলাদেশ
              </div>
              <div className="flex flex-wrap items-center gap-3 font-mono text-xs text-slate-400">
                <span>UI: {typeof window !== 'undefined' ? `${window.location.protocol}//${window.location.host}` : 'http://localhost:3000'}</span>
                <span>•</span>
                <span>API: {typeof window !== 'undefined' ? `${window.location.protocol}//${window.location.hostname}:8080` : 'http://localhost:8080'}</span>
                <span>•</span>
                <span>UTC+6</span>
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
