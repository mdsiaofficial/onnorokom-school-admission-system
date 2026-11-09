import React from 'react';
import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useLanguageStore } from '@/stores/languageStore';
import { useMyHoldsQuery } from '@/api/queries';
import { StudentIdBar } from './StudentIdBar';
import { GraduationCap, BookOpen, UserCheck, PlusCircle, Globe } from 'lucide-react';

export const Header: React.FC = () => {
  const { t } = useTranslation();
  const { language, toggleLanguage } = useLanguageStore();
  const { data: myHolds } = useMyHoldsQuery();

  // Count active holds (status === 'HELD')
  const activeHoldsCount = myHolds?.filter((h) => h.status === 'HELD').length || 0;

  return (
    <header className="sticky top-0 z-40 bg-white border-b border-slate-200 shadow-sm">
      {/* 17-digit Student ID Bar at very top */}
      <StudentIdBar />

      {/* Main Navbar */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Brand Logo & Name */}
          <NavLink to="/" className="flex items-center gap-3 group">
            <div className="w-10 h-10 rounded-xl bg-emerald-600 text-white flex items-center justify-center shadow-md group-hover:bg-emerald-700 transition">
              <GraduationCap className="w-6 h-6" />
            </div>
            <div>
              <span className="text-lg font-bold text-slate-900 group-hover:text-emerald-700 transition block leading-tight">
                {t('app.title')}
              </span>
              <span className="text-xs text-slate-500 hidden sm:block">
                {t('app.subtitle')}
              </span>
            </div>
          </NavLink>

          {/* Navigation links & Language Switcher */}
          <div className="flex items-center gap-1 sm:gap-3">
            <nav className="flex items-center gap-1 sm:gap-2">
              <NavLink
                to="/"
                end
                className={({ isActive }) =>
                  `flex items-center gap-1.5 px-3 py-2 rounded-xl text-sm font-medium transition ${
                    isActive
                      ? 'bg-emerald-50 text-emerald-800 font-semibold'
                      : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                  }`
                }
              >
                <BookOpen className="w-4 h-4" />
                <span>{t('app.nav.classes')}</span>
              </NavLink>

              <NavLink
                to="/my-applications"
                className={({ isActive }) =>
                  `relative flex items-center gap-1.5 px-3 py-2 rounded-xl text-sm font-medium transition ${
                    isActive
                      ? 'bg-emerald-50 text-emerald-800 font-semibold'
                      : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                  }`
                }
              >
                <UserCheck className="w-4 h-4" />
                <span>{t('app.nav.myApplications')}</span>
                {activeHoldsCount > 0 && (
                  <span className="ml-1 inline-flex items-center justify-center px-1.5 py-0.5 text-xs font-bold font-mono leading-none text-white bg-amber-600 rounded-full animate-pulse">
                    {activeHoldsCount}
                  </span>
                )}
              </NavLink>

              <NavLink
                to="/admin/classes/new"
                className={({ isActive }) =>
                  `flex items-center gap-1.5 px-3 py-2 rounded-xl text-sm font-medium transition ${
                    isActive
                      ? 'bg-emerald-50 text-emerald-800 font-semibold'
                      : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                  }`
                }
              >
                <PlusCircle className="w-4 h-4" />
                <span className="hidden md:inline">{t('app.nav.createClass')}</span>
              </NavLink>
            </nav>

            <div className="h-6 w-px bg-slate-200 mx-1"></div>

            {/* Language Toggle Button */}
            <button
              onClick={toggleLanguage}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-xl border border-slate-300 text-slate-700 hover:bg-slate-50 transition shadow-sm"
              title="Change Language"
            >
              <Globe className="w-3.5 h-3.5 text-slate-500" />
              <span>{language === 'bn' ? 'English' : 'বাংলা'}</span>
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};
