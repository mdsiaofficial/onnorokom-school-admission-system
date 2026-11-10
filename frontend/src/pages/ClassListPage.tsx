import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  useClassesQuery,
  useMyHoldsQuery,
  useApplySeatMutation,
  useUpdateClassMutation,
  useDeleteClassMutation,
} from '@/api/queries';
import { useStudentStore } from '@/stores/studentStore';
import { useLanguageStore } from '@/stores/languageStore';
import { useAuthStore } from '@/stores/authStore';
import { formatDhakaTime, formatNumber } from '@/utils/formatters';
import { getLocalizedErrorMessage } from '@/utils/errors';
import { toast } from '@/components/ToastContainer';
import { TableSkeleton, CardSkeleton } from '@/components/LoadingSkeleton';
import { Modal } from '@/components/Modal';
import { ClassSummary } from '@/api/types';
import {
  Users,
  Clock,
  CheckCircle,
  AlertCircle,
  ArrowRight,
  Search,
  RefreshCw,
  Edit2,
  Trash2,
} from 'lucide-react';

export const ClassListPage: React.FC = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { isValid: isStudentIdValid } = useStudentStore();
  const { language } = useLanguageStore();
  const { isTeacher } = useAuthStore();

  const [search, setSearch] = useState('');
  const [lastRefetchTime, setLastRefetchTime] = useState<Date>(new Date());

  const {
    data: classes,
    isLoading,
    isError,
    error,
    dataUpdatedAt,
  } = useClassesQuery();

  const { data: myHolds } = useMyHoldsQuery();
  const applyMutation = useApplySeatMutation();
  const updateMutation = useUpdateClassMutation();
  const deleteMutation = useDeleteClassMutation();

  // State for Edit Modal & Delete Confirm Modal
  const [editingClass, setEditingClass] = useState<ClassSummary | null>(null);
  const [editName, setEditName] = useState('');
  const [editSeats, setEditSeats] = useState('');
  const [editError, setEditError] = useState('');

  const [deletingClass, setDeletingClass] = useState<ClassSummary | null>(null);

  const openEditModal = (cls: ClassSummary) => {
    setEditingClass(cls);
    setEditName(cls.name);
    setEditSeats(cls.totalSeats.toString());
    setEditError('');
  };

  const handleSaveEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingClass) return;

    const trimmed = editName.trim();
    const seatsNum = parseInt(editSeats, 10);
    if (!trimmed || trimmed.length > 120) {
      setEditError(language === 'bn' ? 'নাম 1-120 অক্ষরের মধ্যে হতে হবে।' : 'Name must be 1-120 characters.');
      return;
    }
    if (Number.isNaN(seatsNum) || seatsNum <= 0) {
      setEditError(language === 'bn' ? 'আসন সংখ্যা কমপক্ষে 1 হতে হবে।' : 'Total seats must be at least 1.');
      return;
    }

    try {
      await updateMutation.mutateAsync({
        classId: editingClass.id,
        data: { name: trimmed, totalSeats: seatsNum },
      });
      toast.success(language === 'bn' ? 'ক্লাসের তথ্য সফলভাবে আপডেট করা হয়েছে।' : 'Class updated successfully.');
      setEditingClass(null);
    } catch (err) {
      const msg = getLocalizedErrorMessage(err);
      setEditError(msg);
    }
  };

  const handleConfirmDelete = async () => {
    if (!deletingClass) return;
    try {
      await deleteMutation.mutateAsync(deletingClass.id);
      toast.success(language === 'bn' ? 'ক্লাস সফলভাবে মুছে ফেলা হয়েছে।' : 'Class deleted successfully.');
      setDeletingClass(null);
    } catch (err) {
      const msg = getLocalizedErrorMessage(err);
      toast.error(msg);
    }
  };

  // Keep track of refetch time
  React.useEffect(() => {
    if (dataUpdatedAt) {
      setLastRefetchTime(new Date(dataUpdatedAt));
    }
  }, [dataUpdatedAt]);

  // Map student's active hold or confirmation
  const activeStudentHold = React.useMemo(() => {
    if (myHolds && Array.isArray(myHolds)) {
      return myHolds.find((h) => h.status === 'HELD' || h.status === 'CONFIRMED');
    }
    return null;
  }, [myHolds]);

  const studentClassStatus = React.useMemo(() => {
    const map = new Map<string, 'HELD' | 'CONFIRMED'>();
    if (myHolds && Array.isArray(myHolds)) {
      for (const hold of myHolds) {
        if (hold.status === 'HELD' || hold.status === 'CONFIRMED') {
          map.set(hold.classId, hold.status);
        }
      }
    }
    return map;
  }, [myHolds]);

  const handleApply = async (classId: string, className: string) => {
    if (!isStudentIdValid) {
      toast.error(t('studentBar.requiredNotice'));
      return;
    }

    try {
      await applyMutation.mutateAsync(classId);
      toast.success(t('classes.applySuccess'), 8000);
      navigate('/my-applications');
    } catch (err) {
      const msg = getLocalizedErrorMessage(err);
      toast.error(`${className}: ${msg}`);
    }
  };

  const filteredClasses = React.useMemo(() => {
    if (!classes) return [];
    if (!search.trim()) return classes;
    const term = search.toLowerCase();
    return classes.filter((c) => c.name.toLowerCase().includes(term));
  }, [classes, search]);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Banner when student ID not entered (only when not in teacher mode) */}
      {!isTeacher && !isStudentIdValid && (
        <div className="mb-6 bg-gradient-to-r from-amber-50 to-orange-50 border border-amber-200 rounded-2xl p-4 flex items-start gap-3.5 shadow-sm">
          <AlertCircle className="w-5 h-5 text-amber-600 mt-0.5 shrink-0" />
          <div className="text-sm">
            <span className="font-bold text-amber-900 block mb-0.5">
              {t('studentBar.requiredNotice')}
            </span>
            <span className="text-amber-800">
              {t('studentBar.helper')}
            </span>
          </div>
        </div>
      )}

      {/* Connection error banner */}
      {isError && (
        <div className="mb-6 bg-red-50 border border-red-200 rounded-2xl p-4 flex items-center justify-between gap-3 shadow-sm">
          <div className="flex items-center gap-3">
            <AlertCircle className="w-5 h-5 text-red-600 shrink-0" />
            <span className="text-sm font-medium text-red-800">
              {t('classes.pollingError')} ({getLocalizedErrorMessage(error)})
            </span>
          </div>
        </div>
      )}

      {/* Page Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight flex items-center gap-2">
            <span>{t('classes.title')}</span>
            <span className="inline-flex items-center gap-1 text-xs font-semibold px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 border border-emerald-200">
              <span className="w-2 h-2 rounded-full bg-emerald-500 animate-ping"></span>
              {t('classes.pollingActive')}
            </span>
          </h1>
          <p className="text-slate-600 text-sm mt-1">
            {t('classes.description')}
          </p>
        </div>

        {/* Polling & Search info */}
        <div className="flex items-center gap-3">
          <div className="text-xs text-slate-500 flex items-center gap-1 font-mono">
            <RefreshCw className="w-3.5 h-3.5 text-slate-400" />
            <span>
              {t('classes.lastUpdated', {
                time: formatDhakaTime(lastRefetchTime, language),
              })}
            </span>
          </div>
        </div>
      </div>

      {/* Search Input */}
      <div className="mb-6">
        <div className="relative max-w-md">
          <Search className="w-5 h-5 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('classes.searchPlaceholder')}
            className="w-full bg-white border border-slate-300 rounded-xl pl-10 pr-4 py-2.5 text-sm text-slate-800 placeholder-slate-400 shadow-sm focus:outline-none focus:ring-2 focus:ring-emerald-500/20 focus:border-emerald-600 transition"
          />
        </div>
      </div>

      {/* Loading state */}
      {isLoading ? (
        <div>
          <div className="hidden md:block bg-white rounded-2xl border border-slate-200 shadow-sm p-4">
            <TableSkeleton rows={6} />
          </div>
          <div className="md:hidden">
            <CardSkeleton cards={4} />
          </div>
        </div>
      ) : filteredClasses.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200 p-12 text-center shadow-sm">
          <div className="w-16 h-16 bg-slate-100 rounded-full flex items-center justify-center mx-auto mb-4 text-slate-400">
            <Users className="w-8 h-8" />
          </div>
          <h3 className="text-lg font-bold text-slate-800 mb-1">
            {t('classes.empty')}
          </h3>
          <p className="text-sm text-slate-500">
            {t('classes.description')}
          </p>
        </div>
      ) : (
        <>
          {/* Desktop Table View */}
          <div className="hidden md:block bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50/80">
                <tr>
                  <th
                    scope="col"
                    className="py-3.5 pl-6 pr-3 text-left text-xs font-bold text-slate-600 uppercase tracking-wider"
                  >
                    {t('classes.table.className')}
                  </th>
                  <th
                    scope="col"
                    className="px-3 py-3.5 text-center text-xs font-bold text-slate-600 uppercase tracking-wider"
                  >
                    {t('classes.table.total')}
                  </th>
                  <th
                    scope="col"
                    className="px-3 py-3.5 text-center text-xs font-bold text-slate-600 uppercase tracking-wider"
                  >
                    {t('classes.table.held')}
                  </th>
                  <th
                    scope="col"
                    className="px-3 py-3.5 text-center text-xs font-bold text-slate-600 uppercase tracking-wider"
                  >
                    {t('classes.table.confirmed')}
                  </th>
                  <th
                    scope="col"
                    className="px-3 py-3.5 text-center text-xs font-bold text-slate-600 uppercase tracking-wider"
                  >
                    {t('classes.table.available')}
                  </th>
                  <th
                    scope="col"
                    className="py-3.5 pl-3 pr-6 text-right text-xs font-bold text-slate-600 uppercase tracking-wider"
                  >
                    {isTeacher ? (language === 'bn' ? 'ম্যানেজ করুন' : 'Manage') : t('classes.table.action')}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white">
                {filteredClasses.map((cls) => {
                  const existingStatus = studentClassStatus.get(cls.id);
                  const isAlreadyHeld = existingStatus === 'HELD';
                  const isAlreadyConfirmed = existingStatus === 'CONFIRMED';
                  const isSeatsFull = cls.availableSeats <= 0;
                  const isApplyingThis =
                    applyMutation.isPending &&
                    applyMutation.variables === cls.id;

                  // Disabled reason
                  let disabledReason = '';
                  if (!isStudentIdValid) {
                    disabledReason = t('classes.reasons.enterStudentId');
                  } else if (isAlreadyConfirmed) {
                    disabledReason = t('classes.reasons.alreadyConfirmed');
                  } else if (isAlreadyHeld) {
                    disabledReason = t('classes.reasons.alreadyHeld');
                  } else if (activeStudentHold) {
                    disabledReason = activeStudentHold.status === 'CONFIRMED'
                      ? (language === 'bn' ? `অন্য ক্লাসে ভর্তি নিশ্চিত হয়েছে (${activeStudentHold.className})` : `Already confirmed in ${activeStudentHold.className}`)
                      : (language === 'bn' ? `অন্য ক্লাসে আবেদন চলমান আছে (${activeStudentHold.className})` : `Application pending in ${activeStudentHold.className}`);
                  } else if (isSeatsFull) {
                    disabledReason = t('classes.reasons.noSeats');
                  }

                  const isDisabled = Boolean(disabledReason) || isApplyingThis;

                  return (
                    <tr
                      key={cls.id}
                      className="hover:bg-slate-50/70 transition-colors"
                    >
                      <td className="py-4 pl-6 pr-3 whitespace-nowrap">
                        <div className="font-semibold text-slate-900 text-sm">
                          {cls.name}
                        </div>
                      </td>
                      <td className="px-3 py-4 text-center whitespace-nowrap font-mono text-sm text-slate-700">
                        {formatNumber(cls.totalSeats)}
                      </td>
                      <td className="px-3 py-4 text-center whitespace-nowrap">
                        <span className="inline-flex items-center gap-1 font-mono text-sm font-semibold text-amber-700 bg-amber-50 px-2 py-0.5 rounded-lg border border-amber-200">
                          <Clock className="w-3.5 h-3.5" />
                          {formatNumber(cls.heldSeats)}
                        </span>
                      </td>
                      <td className="px-3 py-4 text-center whitespace-nowrap">
                        <span className="inline-flex items-center gap-1 font-mono text-sm font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-lg border border-emerald-200">
                          <CheckCircle className="w-3.5 h-3.5" />
                          {formatNumber(cls.confirmedSeats)}
                        </span>
                      </td>
                      <td className="px-3 py-4 text-center whitespace-nowrap">
                        <span
                          className={`font-mono text-sm font-bold px-2.5 py-1 rounded-lg ${
                            cls.availableSeats > 0
                              ? 'bg-blue-50 text-blue-700 border border-blue-200'
                              : 'bg-rose-50 text-rose-700 border border-rose-200'
                          }`}
                        >
                          {formatNumber(cls.availableSeats)}
                        </span>
                      </td>
                      <td className="py-4 pl-3 pr-6 text-right whitespace-nowrap">
                        {isTeacher ? (
                          <div className="inline-flex items-center gap-2">
                            <button
                              type="button"
                              onClick={() => openEditModal(cls)}
                              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold bg-indigo-50 text-indigo-700 hover:bg-indigo-100 border border-indigo-200 transition shadow-sm"
                            >
                              <Edit2 className="w-3.5 h-3.5" />
                              <span>{language === 'bn' ? 'সম্পাদনা' : 'Edit'}</span>
                            </button>
                            <button
                              type="button"
                              onClick={() => setDeletingClass(cls)}
                              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold bg-rose-50 text-rose-700 hover:bg-rose-100 border border-rose-200 transition shadow-sm"
                            >
                              <Trash2 className="w-3.5 h-3.5" />
                              <span>{language === 'bn' ? 'মুছুন' : 'Delete'}</span>
                            </button>
                          </div>
                        ) : (
                          <div className="inline-flex flex-col items-end">
                            <button
                              type="button"
                              disabled={isDisabled}
                              onClick={() => handleApply(cls.id, cls.name)}
                              className={`inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-sm font-semibold transition-all shadow-sm ${
                                isDisabled
                                  ? 'bg-slate-100 text-slate-400 cursor-not-allowed border border-slate-200'
                                  : 'bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white shadow-emerald-600/20 hover:shadow-md'
                              }`}
                              title={disabledReason || undefined}
                            >
                              {isApplyingThis ? (
                                <>
                                  <RefreshCw className="w-4 h-4 animate-spin" />
                                  <span>{t('classes.applying')}</span>
                                </>
                              ) : (
                                <>
                                  <span>{t('classes.applyButton')}</span>
                                  <ArrowRight className="w-4 h-4" />
                                </>
                              )}
                            </button>
                            {disabledReason && (
                              <span className="text-[11px] text-slate-500 mt-1 max-w-[200px] text-right truncate">
                                {disabledReason}
                              </span>
                            )}
                          </div>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Mobile Cards View */}
          <div className="md:hidden grid grid-cols-1 gap-4">
            {filteredClasses.map((cls) => {
              const existingStatus = studentClassStatus.get(cls.id);
              const isAlreadyHeld = existingStatus === 'HELD';
              const isAlreadyConfirmed = existingStatus === 'CONFIRMED';
              const isSeatsFull = cls.availableSeats <= 0;
              const isApplyingThis =
                applyMutation.isPending &&
                applyMutation.variables === cls.id;

              let disabledReason = '';
              if (!isStudentIdValid) {
                disabledReason = t('classes.reasons.enterStudentId');
              } else if (isAlreadyConfirmed) {
                disabledReason = t('classes.reasons.alreadyConfirmed');
              } else if (isAlreadyHeld) {
                disabledReason = t('classes.reasons.alreadyHeld');
              } else if (activeStudentHold) {
                disabledReason = activeStudentHold.status === 'CONFIRMED'
                  ? (language === 'bn' ? `অন্য ক্লাসে ভর্তি নিশ্চিত হয়েছে (${activeStudentHold.className})` : `Already confirmed in ${activeStudentHold.className}`)
                  : (language === 'bn' ? `অন্য ক্লাসে আবেদন চলমান আছে (${activeStudentHold.className})` : `Application pending in ${activeStudentHold.className}`);
              } else if (isSeatsFull) {
                disabledReason = t('classes.reasons.noSeats');
              }

              const isDisabled = Boolean(disabledReason) || isApplyingThis;

              return (
                <div
                  key={cls.id}
                  className="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm space-y-4"
                >
                  <div className="flex items-start justify-between gap-2">
                    <h3 className="font-bold text-slate-900 text-base leading-snug">
                      {cls.name}
                    </h3>
                    <span
                      className={`shrink-0 font-mono text-xs font-bold px-2 py-0.5 rounded-full ${
                        cls.availableSeats > 0
                          ? 'bg-blue-50 text-blue-700 border border-blue-200'
                          : 'bg-rose-50 text-rose-700 border border-rose-200'
                      }`}
                    >
                      {formatNumber(cls.availableSeats)} {t('common.seats')}
                    </span>
                  </div>

                  {/* Seat counts grid */}
                  <div className="grid grid-cols-3 gap-2 bg-slate-50 rounded-xl p-3 text-center border border-slate-100">
                    <div>
                      <span className="block text-[11px] font-medium text-slate-500 mb-0.5">
                        {t('classes.table.total')}
                      </span>
                      <span className="font-mono text-sm font-bold text-slate-800">
                        {formatNumber(cls.totalSeats)}
                      </span>
                    </div>
                    <div>
                      <span className="block text-[11px] font-medium text-slate-500 mb-0.5">
                        {t('classes.table.held')}
                      </span>
                      <span className="font-mono text-sm font-bold text-amber-700">
                        {formatNumber(cls.heldSeats)}
                      </span>
                    </div>
                    <div>
                      <span className="block text-[11px] font-medium text-slate-500 mb-0.5">
                        {t('classes.table.confirmed')}
                      </span>
                      <span className="font-mono text-sm font-bold text-emerald-700">
                        {formatNumber(cls.confirmedSeats)}
                      </span>
                    </div>
                  </div>

                  {isTeacher ? (
                    <div className="flex items-center gap-2 pt-2 border-t border-slate-100">
                      <button
                        type="button"
                        onClick={() => openEditModal(cls)}
                        className="flex-1 inline-flex items-center justify-center gap-1.5 py-2 px-3 rounded-xl text-xs font-semibold bg-indigo-50 text-indigo-700 hover:bg-indigo-100 border border-indigo-200 transition"
                      >
                        <Edit2 className="w-3.5 h-3.5" />
                        <span>{language === 'bn' ? 'সম্পাদনা' : 'Edit'}</span>
                      </button>
                      <button
                        type="button"
                        onClick={() => setDeletingClass(cls)}
                        className="flex-1 inline-flex items-center justify-center gap-1.5 py-2 px-3 rounded-xl text-xs font-semibold bg-rose-50 text-rose-700 hover:bg-rose-100 border border-rose-200 transition"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                        <span>{language === 'bn' ? 'মুছুন' : 'Delete'}</span>
                      </button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      disabled={isDisabled}
                      onClick={() => handleApply(cls.id, cls.name)}
                      className={`w-full inline-flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl text-sm font-semibold transition-all shadow-sm ${
                        isDisabled
                          ? 'bg-slate-100 text-slate-400 cursor-not-allowed border border-slate-200'
                          : 'bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white'
                      }`}
                    >
                      {isApplyingThis ? (
                        <>
                          <RefreshCw className="w-4 h-4 animate-spin" />
                          <span>{t('classes.applying')}</span>
                        </>
                      ) : (
                        <>
                          <span>{t('classes.applyButton')}</span>
                          <ArrowRight className="w-4 h-4" />
                        </>
                      )}
                    </button>
                  )}

                  {disabledReason && (
                    <p className="text-xs text-center text-slate-500">
                      {disabledReason}
                    </p>
                  )}
                </div>
              );
            })}
          </div>
        </>
      )}

      {/* Teacher Edit Class Modal */}
      {editingClass && (
        <Modal
          isOpen={Boolean(editingClass)}
          onClose={() => setEditingClass(null)}
          title={language === 'bn' ? 'ক্লাসের তথ্য সম্পাদনা' : 'Edit Class Details'}
        >
          <form onSubmit={handleSaveEdit} className="space-y-4">
            {editError && (
              <div className="p-3 text-xs bg-rose-50 border border-rose-200 text-rose-700 rounded-xl">
                {editError}
              </div>
            )}
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                {language === 'bn' ? 'ক্লাসের নাম ও শিফট' : 'Class Name & Shift'}
              </label>
              <input
                type="text"
                value={editName}
                onChange={(e) => setEditName(e.target.value)}
                className="w-full px-3 py-2 text-sm border border-slate-300 rounded-xl focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                maxLength={120}
                required
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                {language === 'bn' ? 'মোট আসন সংখ্যা' : 'Total Seats'}
              </label>
              <input
                type="number"
                min={editingClass.heldSeats + editingClass.confirmedSeats}
                value={editSeats}
                onChange={(e) => setEditSeats(e.target.value)}
                className="w-full px-3 py-2 text-sm border border-slate-300 rounded-xl focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                required
              />
              <p className="text-[11px] text-slate-500 mt-1">
                {language === 'bn'
                  ? `বর্তমানে ${formatNumber(editingClass.heldSeats + editingClass.confirmedSeats)} টি আসন দখলকৃত (${formatNumber(editingClass.heldSeats)} আবেদন + ${formatNumber(editingClass.confirmedSeats)} নিশ্চিত)।`
                  : `Currently ${editingClass.heldSeats + editingClass.confirmedSeats} seats committed (${editingClass.heldSeats} held + ${editingClass.confirmedSeats} confirmed).`}
              </p>
            </div>
            <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setEditingClass(null)}
                className="px-4 py-2 text-xs font-semibold text-slate-600 hover:text-slate-800 rounded-xl"
              >
                {language === 'bn' ? 'বাতিল' : 'Cancel'}
              </button>
              <button
                type="submit"
                disabled={updateMutation.isPending}
                className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-bold bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl shadow-sm transition disabled:opacity-50"
              >
                {updateMutation.isPending && <RefreshCw className="w-3.5 h-3.5 animate-spin" />}
                <span>{language === 'bn' ? 'সংরক্ষণ করুন' : 'Save Changes'}</span>
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Teacher Delete Class Confirm Modal */}
      {deletingClass && (
        <Modal
          isOpen={Boolean(deletingClass)}
          onClose={() => setDeletingClass(null)}
          title={language === 'bn' ? 'ক্লাস মুছে ফেলার নিশ্চিতকরণ' : 'Confirm Class Deletion'}
        >
          <div className="space-y-4">
            <p className="text-sm text-slate-600">
              {language === 'bn'
                ? `আপনি কি নিশ্চিত যে '${deletingClass.name}' ক্লাসটি মুছে ফেলতে চান?`
                : `Are you sure you want to delete '${deletingClass.name}'?`}
            </p>
            {deletingClass.heldSeats + deletingClass.confirmedSeats > 0 && (
              <div className="p-3 text-xs bg-amber-50 border border-amber-200 text-amber-800 rounded-xl">
                {language === 'bn'
                  ? 'সতর্কতা: এই ক্লাসে সক্রিয় আবেদন বা নিশ্চিত শিক্ষার্থী রয়েছে। চলমান আবেদন শেষ না হওয়া পর্যন্ত এটি মুছে ফেলা যাবে না।'
                  : 'Warning: This class has active holds or confirmed students and cannot be deleted until they are resolved.'}
              </div>
            )}
            <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setDeletingClass(null)}
                className="px-4 py-2 text-xs font-semibold text-slate-600 hover:text-slate-800 rounded-xl"
              >
                {language === 'bn' ? 'বাতিল' : 'Cancel'}
              </button>
              <button
                type="button"
                disabled={deleteMutation.isPending}
                onClick={handleConfirmDelete}
                className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-bold bg-rose-600 hover:bg-rose-700 text-white rounded-xl shadow-sm transition disabled:opacity-50"
              >
                {deleteMutation.isPending && <RefreshCw className="w-3.5 h-3.5 animate-spin" />}
                <span>{language === 'bn' ? 'হ্যাঁ, মুছুন' : 'Yes, Delete'}</span>
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
};
