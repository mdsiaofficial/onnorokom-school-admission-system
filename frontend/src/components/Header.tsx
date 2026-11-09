import React from 'react';
import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useLanguageStore } from '@/stores/languageStore';
import { useAuthStore } from '@/stores/authStore';
import { useMyHoldsQuery } from '@/api/queries';
import { StudentIdBar } from './StudentIdBar';
import { GraduationCap, BookOpen, UserCheck, PlusCircle, Globe, LogIn, LogOut, UserCheck2 } from 'lucide-react';

export const Header: React.FC = () => {
  const { t } = useTranslation();
  const { language, toggleLanguage } = useLanguageStore();
  const { isTeacher, teacherName, loginAsTeacher, logout } = useAuthStore();
  const { data: myHolds } = useMyHoldsQuery();

  // Count active holds (status === 'HELD')
  const activeHoldsCount = myHolds?.filter((h) => h.status === 'HELD').length || 0;

  return (
    <header className="sticky top-0 z-40 bg-white border-b border-slate-200 shadow-sm">
      {/* 17-digit Student ID Bar at very top (only for students, hidden when teacher logged in) */}
      {!isTeacher && <StudentIdBar />}

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

          {/* Navigation links, Teacher Demo Login & Language Switcher */}
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

              {/* Show My Applications only for students (hidden when Teacher is logged in) */}
              {!isTeacher && (
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
              )}

              {/* Show Create Class only if Teacher is logged in */}
              {isTeacher && (
                <NavLink
                  to="/admin/classes/new"
                  className={({ isActive }) =>
                    `flex items-center gap-1.5 px-3 py-2 rounded-xl text-sm font-medium transition ${
                      isActive
                        ? 'bg-indigo-50 text-indigo-800 font-semibold border border-indigo-200'
                        : 'text-indigo-700 bg-indigo-50/70 hover:bg-indigo-100'
                    }`
                  }
                >
                  <PlusCircle className="w-4 h-4" />
                  <span className="font-semibold">{t('app.nav.createClass')}</span>
                </NavLink>
              )}
            </nav>

            <div className="h-6 w-px bg-slate-200 mx-1"></div>

            {/* Teacher One-Click Demo Login Button */}
            {isTeacher ? (
              <div className="flex items-center gap-2 bg-indigo-50 border border-indigo-200 rounded-xl px-2.5 py-1">
                <div className="hidden lg:flex flex-col text-left">
                  <span className="text-[10px] uppercase font-bold tracking-wider text-indigo-600 flex items-center gap-1">
                    <UserCheck2 className="w-3 h-3" /> শিক্ষক প্যানেল
                  </span>
                  <span className="text-xs font-medium text-indigo-950 truncate max-w-[120px]">
                    {teacherName}
                  </span>
                </div>
                <button
                  onClick={logout}
                  className="flex items-center gap-1 text-xs font-medium text-rose-600 hover:text-rose-700 hover:bg-rose-50 px-2 py-1 rounded-lg transition"
                  title="লগআউট"
                >
                  <LogOut className="w-3.5 h-3.5" />
                  <span className="hidden sm:inline">লগআউট</span>
                </button>
              </div>
            ) : (
              <button
                onClick={loginAsTeacher}
                className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold rounded-xl bg-gradient-to-r from-indigo-600 to-indigo-700 hover:from-indigo-700 hover:to-indigo-800 text-white shadow-sm transition active:scale-95"
                title="শিক্ষক ডেমো লগইন করুন"
              >
                <LogIn className="w-3.5 h-3.5" />
                <span>শিক্ষক লগইন (Demo)</span>
              </button>
            )}

            <div className="h-6 w-px bg-slate-200 mx-0.5"></div>

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
