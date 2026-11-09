import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  useMyHoldsQuery,
  useConfirmHoldMutation,
  useCancelHoldMutation,
} from '@/api/queries';
import { useStudentStore } from '@/stores/studentStore';
import { StudentHold } from '@/api/types';
import { HoldItem } from '@/components/HoldItem';
import { Modal } from '@/components/Modal';
import { toast } from '@/components/ToastContainer';
import { getLocalizedErrorMessage } from '@/utils/errors';
import { CardSkeleton } from '@/components/LoadingSkeleton';
import {
  UserCheck,
  AlertCircle,
  FolderOpen,
  ArrowRight,
  ShieldAlert,
} from 'lucide-react';

export const MyApplicationsPage: React.FC = () => {
  const { t } = useTranslation();
  const { studentId, isValid: isStudentIdValid } = useStudentStore();

  const { data: holds, isLoading, isError, error } = useMyHoldsQuery();
  const confirmMutation = useConfirmHoldMutation();
  const cancelMutation = useCancelHoldMutation();

  // State for cancel confirmation modal
  const [selectedHoldForCancel, setSelectedHoldForCancel] = useState<StudentHold | null>(null);

  const handlePay = async (holdId: string, className: string) => {
    try {
      await confirmMutation.mutateAsync(holdId);
      toast.success(
        `${className}: ${t('myApplications.alerts.confirmSuccess')}`,
        6000
      );
    } catch (err) {
      const msg = getLocalizedErrorMessage(err);
      toast.error(`${className}: ${msg}`);
    }
  };

  const handleConfirmCancel = async () => {
    if (!selectedHoldForCancel) return;
    const { holdId, className } = selectedHoldForCancel;

    try {
      await cancelMutation.mutateAsync(holdId);
      toast.success(
        `${className}: ${t('myApplications.alerts.cancelSuccess')}`
      );
    } catch (err) {
      const msg = getLocalizedErrorMessage(err);
      toast.error(`${className}: ${msg}`);
    } finally {
      setSelectedHoldForCancel(null);
    }
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Page Header */}
      <div className="mb-8">
        <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight flex items-center gap-2.5">
          <UserCheck className="w-8 h-8 text-emerald-600" />
          <span>{t('myApplications.title')}</span>
        </h1>
        <p className="text-slate-600 text-sm mt-1">
          {t('myApplications.description')}
        </p>
      </div>

      {/* Case 1: No valid student ID */}
      {!isStudentIdValid ? (
        <div className="bg-white rounded-2xl border border-amber-200 p-8 text-center max-w-lg mx-auto shadow-sm">
          <div className="w-16 h-16 bg-amber-50 text-amber-600 rounded-full flex items-center justify-center mx-auto mb-4">
            <AlertCircle className="w-8 h-8" />
          </div>
          <h2 className="text-lg font-bold text-slate-900 mb-2">
            {t('myApplications.noStudentId')}
          </h2>
          <p className="text-slate-600 text-sm mb-6">
            {t('studentBar.helper')}
          </p>
          <div className="inline-flex items-center gap-2 text-xs font-semibold text-emerald-700 bg-emerald-50 px-4 py-2 rounded-xl border border-emerald-200">
            <span>উপরে স্ক্রিনের শীর্ষবারে 17 অঙ্কের নম্বর প্রবেশ করান</span>
          </div>
        </div>
      ) : isLoading ? (
        /* Case 2: Loading */
        <div className="space-y-4">
          <CardSkeleton cards={3} />
        </div>
      ) : isError ? (
        /* Case 3: Error */
        <div className="bg-red-50 border border-red-200 rounded-2xl p-6 text-center text-red-900 shadow-sm">
          <ShieldAlert className="w-10 h-10 text-red-600 mx-auto mb-2" />
          <h3 className="font-bold text-base mb-1">
            {getLocalizedErrorMessage(error)}
          </h3>
        </div>
      ) : !holds || holds.length === 0 ? (
        /* Case 4: Empty applications */
        <div className="bg-white rounded-2xl border border-slate-200 p-12 text-center max-w-lg mx-auto shadow-sm">
          <div className="w-16 h-16 bg-slate-100 text-slate-400 rounded-full flex items-center justify-center mx-auto mb-4">
            <FolderOpen className="w-8 h-8" />
          </div>
          <h2 className="text-lg font-bold text-slate-900 mb-2">
            {t('myApplications.empty')}
          </h2>
          <p className="text-slate-500 text-sm mb-6">
            বর্তমান শিক্ষার্থী আইডি: <span className="font-mono font-bold text-slate-700">{studentId}</span>
          </p>
          <Link
            to="/"
            className="inline-flex items-center gap-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-sm px-5 py-2.5 rounded-xl shadow-sm transition"
          >
            <span>{t('myApplications.goToClasses')}</span>
            <ArrowRight className="w-4 h-4" />
          </Link>
        </div>
      ) : (
        /* Case 5: Holds list */
        <div className="space-y-4">
          {holds.map((hold) => (
            <HoldItem
              key={hold.holdId}
              hold={hold}
              onPay={handlePay}
              onCancelRequest={(h) => setSelectedHoldForCancel(h)}
              isPaying={
                confirmMutation.isPending &&
                confirmMutation.variables === hold.holdId
              }
            />
          ))}
        </div>
      )}

      {/* Cancel Confirmation Modal */}
      <Modal
        isOpen={Boolean(selectedHoldForCancel)}
        onClose={() => setSelectedHoldForCancel(null)}
        title={t('myApplications.cancelConfirmModal.title')}
      >
        <div className="space-y-4">
          <p className="text-sm text-slate-600 leading-relaxed">
            {t('myApplications.cancelConfirmModal.message', {
              className: selectedHoldForCancel?.className,
            })}
          </p>

          <div className="flex items-center justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={() => setSelectedHoldForCancel(null)}
              className="px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100 rounded-xl transition"
            >
              {t('myApplications.cancelConfirmModal.cancel')}
            </button>
            <button
              type="button"
              disabled={cancelMutation.isPending}
              onClick={handleConfirmCancel}
              className="px-4 py-2 text-sm font-semibold text-white bg-red-600 hover:bg-red-700 active:bg-red-800 disabled:opacity-50 rounded-xl shadow-sm transition"
            >
              {cancelMutation.isPending
                ? t('myApplications.actions.cancelling')
                : t('myApplications.cancelConfirmModal.confirm')}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
};
