import React from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AlertCircle, Home } from 'lucide-react';

export const NotFoundPage: React.FC = () => {
  const { t } = useTranslation();

  return (
    <div className="max-w-md mx-auto px-4 py-16 text-center">
      <div className="w-16 h-16 bg-slate-100 text-slate-500 rounded-full flex items-center justify-center mx-auto mb-4">
        <AlertCircle className="w-8 h-8" />
      </div>
      <h1 className="text-2xl font-bold text-slate-900 mb-2">{t('notFound.title')}</h1>
      <p className="text-slate-600 text-sm mb-6">{t('notFound.body')}</p>
      <Link
        to="/"
        className="inline-flex items-center gap-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-sm px-5 py-2.5 rounded-xl shadow-sm transition"
      >
        <Home className="w-4 h-4" />
        <span>{t('app.nav.classes')}</span>
      </Link>
    </div>
  );
};
