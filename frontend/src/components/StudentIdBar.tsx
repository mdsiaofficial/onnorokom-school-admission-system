import React from 'react';
import { useTranslation } from 'react-i18next';
import { useStudentStore } from '@/stores/studentStore';
import { CheckCircle2, AlertCircle, X, ShieldCheck } from 'lucide-react';

export const StudentIdBar: React.FC = () => {
  const { t } = useTranslation();
  const { studentId, setStudentId, clearStudentId, isValid } = useStudentStore();

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setStudentId(e.target.value);
  };

  const length = studentId.length;

  return (
    <div className="bg-slate-900 text-slate-100 py-3 px-4 border-b border-slate-800 shadow-md">
      <div className="max-w-7xl mx-auto flex flex-col md:flex-row items-center justify-between gap-3">
        {/* Left helper label */}
        <div className="flex items-center gap-2 text-xs md:text-sm text-slate-300 w-full md:w-auto">
          <ShieldCheck className="w-5 h-5 text-emerald-400 shrink-0" />
          <span className="font-medium text-slate-200">
            {t('studentBar.label')}
          </span>
          <span className="hidden lg:inline text-slate-400">
            ({t('studentBar.helper')})
          </span>
        </div>

        {/* Right input & counter */}
        <div className="flex items-center gap-2 w-full md:w-auto">
          <div className="relative flex-1 md:w-80">
            <input
              type="text"
              inputMode="numeric"
              pattern="[0-9]*"
              maxLength={17}
              value={studentId}
              onChange={handleChange}
              placeholder={t('studentBar.placeholder')}
              className={`w-full bg-slate-800 text-white placeholder-slate-400 text-sm font-mono tracking-wider px-3.5 py-2 pr-20 rounded-xl border transition-all focus:outline-none focus:ring-2 ${
                isValid
                  ? 'border-emerald-500 focus:ring-emerald-500/30'
                  : length > 0
                  ? 'border-amber-500 focus:ring-amber-500/30'
                  : 'border-slate-700 focus:border-slate-500 focus:ring-slate-500/20'
              }`}
            />

            {/* Counter and clear button inside input */}
            <div className="absolute right-2.5 top-1/2 -translate-y-1/2 flex items-center gap-1.5 pointer-events-auto">
              <span
                className={`text-xs font-mono font-semibold px-1.5 py-0.5 rounded ${
                  isValid
                    ? 'bg-emerald-950 text-emerald-400'
                    : length > 0
                    ? 'bg-amber-950 text-amber-400'
                    : 'text-slate-500'
                }`}
              >
                {length}/17
              </span>

              {length > 0 && (
                <button
                  type="button"
                  onClick={clearStudentId}
                  className="text-slate-400 hover:text-white p-0.5 rounded transition"
                  title={t('studentBar.clear')}
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              )}
            </div>
          </div>

          {/* Validity status icon */}
          <div className="shrink-0 flex items-center">
            {isValid ? (
              <div
                className="flex items-center gap-1 text-emerald-400 text-xs font-medium bg-emerald-950/60 border border-emerald-800/60 px-2.5 py-1.5 rounded-lg"
                title={t('studentBar.valid')}
              >
                <CheckCircle2 className="w-4 h-4" />
                <span className="hidden sm:inline">{t('studentBar.valid')}</span>
              </div>
            ) : length > 0 ? (
              <div
                className="flex items-center gap-1 text-amber-400 text-xs font-medium bg-amber-950/60 border border-amber-800/60 px-2.5 py-1.5 rounded-lg"
                title={t('studentBar.incomplete')}
              >
                <AlertCircle className="w-4 h-4" />
                <span className="hidden sm:inline">{t('studentBar.incomplete')}</span>
              </div>
            ) : null}
          </div>
        </div>
      </div>
    </div>
  );
};
