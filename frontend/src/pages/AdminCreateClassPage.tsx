import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useCreateClassMutation } from '@/api/queries';
import { useAuthStore } from '@/stores/authStore';
import { toast } from '@/components/ToastContainer';
import { getLocalizedErrorMessage } from '@/utils/errors';
import { PlusCircle, School, Users, ArrowLeft, RefreshCw, Lock, LogIn, UserCheck2 } from 'lucide-react';

export const AdminCreateClassPage: React.FC = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { isTeacher, teacherName, loginAsTeacher } = useAuthStore();
  const createClassMutation = useCreateClassMutation();

  const [name, setName] = useState('');
  const [totalSeats, setTotalSeats] = useState<string>('60');
  const [errors, setErrors] = useState<{ name?: string; totalSeats?: string }>({});

  const validate = (): boolean => {
    const errs: { name?: string; totalSeats?: string } = {};

    const trimmed = name.trim();
    if (!trimmed) {
      errs.name = t('admin.form.errors.nameRequired');
    } else if (trimmed.length > 120) {
      errs.name = t('admin.form.errors.nameTooLong');
    }

    const seatsNum = parseInt(totalSeats, 10);
    if (!totalSeats || Number.isNaN(seatsNum) || seatsNum <= 0) {
      errs.totalSeats = t('admin.form.errors.seatsInvalid');
    }

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    try {
      await createClassMutation.mutateAsync({
        name: name.trim(),
        totalSeats: parseInt(totalSeats, 10),
      });

      toast.success(t('admin.form.success'));
      navigate('/');
    } catch (err) {
      const msg = getLocalizedErrorMessage(err);
      toast.error(msg);
    }
  };

  if (!isTeacher) {
    return (
      <div className="max-w-xl mx-auto px-4 py-16 text-center">
        <div className="bg-white rounded-3xl border border-slate-200 p-8 shadow-sm">
          <div className="w-16 h-16 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center mx-auto mb-4">
            <Lock className="w-8 h-8" />
          </div>
          <h2 className="text-xl font-bold text-slate-900 mb-2">
            {t('admin.gate.title')}
          </h2>
          <p className="text-sm text-slate-500 mb-6 leading-relaxed">
            {t('admin.gate.body')}
          </p>
          <div className="flex flex-col sm:flex-row items-center justify-center gap-3">
            <button
              onClick={loginAsTeacher}
              className="w-full sm:w-auto inline-flex items-center justify-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-2.5 px-6 rounded-xl shadow-md transition"
            >
              <LogIn className="w-4 h-4" />
              <span>{t('admin.gate.login')}</span>
            </button>
            <button
              onClick={() => navigate('/')}
              className="w-full sm:w-auto text-slate-600 hover:text-slate-900 font-semibold py-2.5 px-4 text-sm"
            >
              {t('admin.gate.back')}
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Back button */}
      <div className="flex items-center justify-between mb-6">
        <button
          type="button"
          onClick={() => navigate('/')}
          className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-500 hover:text-slate-800 transition"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>{t('admin.backToList')}</span>
        </button>

        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-indigo-700 bg-indigo-50 border border-indigo-200 px-3 py-1 rounded-full">
          <UserCheck2 className="w-3.5 h-3.5" />
          <span>{t('admin.loggedInAs', { name: teacherName })}</span>
        </span>
      </div>

      {/* Card Header */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 sm:p-8">
        <div className="flex items-center gap-3 mb-2">
          <div className="w-12 h-12 bg-emerald-50 text-emerald-700 rounded-xl flex items-center justify-center">
            <PlusCircle className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl sm:text-2xl font-bold text-slate-900">
              {t('admin.title')}
            </h1>
            <p className="text-xs sm:text-sm text-slate-500">
              {t('admin.description')}
            </p>
          </div>
        </div>

        <hr className="my-6 border-slate-100" />

        {/* Create Class Form */}
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Class Name */}
          <div>
            <label
              htmlFor="class-name"
              className="block text-sm font-semibold text-slate-800 mb-1.5 flex items-center gap-1.5"
            >
              <School className="w-4 h-4 text-emerald-600" />
              <span>{t('admin.form.nameLabel')}</span>
            </label>
            <input
              id="class-name"
              type="text"
              maxLength={120}
              value={name}
              onChange={(e) => {
                setName(e.target.value);
                if (errors.name) setErrors((prev) => ({ ...prev, name: undefined }));
              }}
              placeholder={t('admin.form.namePlaceholder')}
              className={`w-full px-4 py-2.5 rounded-xl border text-sm text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 transition ${
                errors.name
                  ? 'border-red-400 focus:ring-red-500/20'
                  : 'border-slate-300 focus:border-emerald-600 focus:ring-emerald-500/20'
              }`}
            />
            {errors.name ? (
              <p className="text-xs text-red-600 mt-1 font-medium">{errors.name}</p>
            ) : (
              <p className="text-xs text-slate-500 mt-1">
                {t('admin.form.nameHelp')}
              </p>
            )}
          </div>

          {/* Total Seats */}
          <div>
            <label
              htmlFor="total-seats"
              className="block text-sm font-semibold text-slate-800 mb-1.5 flex items-center gap-1.5"
            >
              <Users className="w-4 h-4 text-emerald-600" />
              <span>{t('admin.form.seatsLabel')}</span>
            </label>
            <input
              id="total-seats"
              type="number"
              min={1}
              max={1000}
              value={totalSeats}
              onChange={(e) => {
                setTotalSeats(e.target.value);
                if (errors.totalSeats)
                  setErrors((prev) => ({ ...prev, totalSeats: undefined }));
              }}
              placeholder={t('admin.form.seatsPlaceholder')}
              className={`w-full px-4 py-2.5 rounded-xl border text-sm text-slate-900 font-mono placeholder-slate-400 focus:outline-none focus:ring-2 transition ${
                errors.totalSeats
                  ? 'border-red-400 focus:ring-red-500/20'
                  : 'border-slate-300 focus:border-emerald-600 focus:ring-emerald-500/20'
              }`}
            />
            {errors.totalSeats ? (
              <p className="text-xs text-red-600 mt-1 font-medium">{errors.totalSeats}</p>
            ) : (
              <p className="text-xs text-slate-500 mt-1">
                {t('admin.form.seatsHelp')}
              </p>
            )}
          </div>

          {/* Submit Button */}
          <div className="pt-2">
            <button
              type="submit"
              disabled={createClassMutation.isPending}
              className="w-full inline-flex items-center justify-center gap-2 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 disabled:opacity-50 text-white font-semibold py-3 px-5 rounded-xl shadow-md shadow-emerald-600/20 transition text-sm"
            >
              {createClassMutation.isPending ? (
                <>
                  <RefreshCw className="w-4 h-4 animate-spin" />
                  <span>{t('admin.form.submitting')}</span>
                </>
              ) : (
                <>
                  <PlusCircle className="w-4 h-4" />
                  <span>{t('admin.form.submit')}</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
