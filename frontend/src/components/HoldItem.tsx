import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import { StudentHold, HoldStatus } from '@/api/types';
import { useCountdown } from '@/utils/countdown';
import { useLanguageStore } from '@/stores/languageStore';
import { formatDhakaDateTime } from '@/utils/formatters';
import { StatusBadge } from './Badge';
import { Clock, CreditCard, Ban, CheckCircle2, AlertOctagon } from 'lucide-react';

interface HoldItemProps {
  hold: StudentHold;
  onPay: (holdId: string, className: string) => Promise<void>;
  onCancelRequest: (hold: StudentHold) => void;
  isPaying: boolean;
}

export const HoldItem: React.FC<HoldItemProps> = ({
  hold,
  onPay,
  onCancelRequest,
  isPaying,
}) => {
  const { t } = useTranslation();
  const { language } = useLanguageStore();
  const queryClient = useQueryClient();

  // Local status flip when timer expires at 0 without page reload
  const [localStatus, setLocalStatus] = useState<HoldStatus>(hold.status);

  // Sync with prop when prop changes from server
  React.useEffect(() => {
    setLocalStatus(hold.status);
  }, [hold.status]);

  const handleTimerExpire = React.useCallback(() => {
    setLocalStatus('EXPIRED');
    // Immediate background refetch to reconcile with server state
    queryClient.invalidateQueries({ queryKey: ['my-holds'] });
    queryClient.invalidateQueries({ queryKey: ['classes'] });
  }, [queryClient]);

  const countdown = useCountdown(
    hold.expiresAt,
    localStatus === 'HELD' ? handleTimerExpire : undefined
  );

  const effectiveStatus: HoldStatus =
    localStatus === 'HELD' && countdown.isExpired ? 'EXPIRED' : localStatus;

  const isHeld = effectiveStatus === 'HELD';
  const isConfirmed = effectiveStatus === 'CONFIRMED';
  const isExpired = effectiveStatus === 'EXPIRED';
  const isCancelled = effectiveStatus === 'CANCELLED';

  return (
    <div
      className={`bg-white rounded-2xl border transition-all p-5 shadow-sm ${
        isHeld
          ? 'border-amber-200 ring-1 ring-amber-200/50 bg-gradient-to-br from-white to-amber-50/20'
          : isConfirmed
          ? 'border-emerald-200 bg-gradient-to-br from-white to-emerald-50/20'
          : 'border-slate-200'
      }`}
    >
      {/* Live region kept mounted so the expiry is announced when the countdown ends. */}
      <span role="status" aria-live="polite" className="sr-only">
        {isExpired ? t('myApplications.expiredAnnouncement', { className: hold.className }) : ''}
      </span>
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        {/* Left: Class name & details */}
        <div className="space-y-1.5">
          <div className="flex items-center gap-2.5 flex-wrap">
            <h3 className="text-base sm:text-lg font-bold text-slate-900">
              {hold.className}
            </h3>
            <StatusBadge status={effectiveStatus} />
          </div>

          <div className="flex items-center gap-4 text-xs text-slate-500 font-mono">
            <span>
              {t('myApplications.table.expiry')}:{' '}
              {formatDhakaDateTime(hold.expiresAt, language)}
            </span>
          </div>
        </div>

        {/* Center: Live countdown for HELD status */}
        {isHeld && (
          <div className="flex items-center gap-2 bg-amber-50 border border-amber-200/80 rounded-xl px-4 py-2 text-amber-900">
            <Clock className="w-5 h-5 text-amber-600 animate-pulse motion-reduce:animate-none shrink-0" />
            <div>
              <div className="text-[11px] font-medium text-amber-700 uppercase tracking-wide">
                {t('myApplications.table.timeRemaining')}
              </div>
              <div className="font-mono text-lg font-extrabold text-amber-900 tracking-wider">
                {countdown.formatted}
              </div>
            </div>
          </div>
        )}

        {/* Right: Actions */}
        <div className="flex items-center gap-2 flex-wrap">
          {isHeld && (
            <>
              {/* Confirm / Pay Button */}
              <button
                type="button"
                disabled={isPaying || countdown.isExpired}
                onClick={() => onPay(hold.holdId, hold.className)}
                className="inline-flex items-center gap-1.5 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 disabled:opacity-50 text-white font-semibold text-sm px-4 py-2.5 rounded-xl shadow-sm shadow-emerald-600/20 transition"
              >
                <CreditCard className="w-4 h-4" />
                <span>
                  {isPaying
                    ? t('myApplications.actions.paying')
                    : t('myApplications.actions.pay')}
                </span>
              </button>

              {/* Cancel Button */}
              <button
                type="button"
                disabled={isPaying}
                onClick={() => onCancelRequest(hold)}
                className="inline-flex items-center gap-1.5 bg-white hover:bg-red-50 text-red-600 border border-red-200 hover:border-red-300 font-medium text-sm px-3.5 py-2.5 rounded-xl transition"
              >
                <Ban className="w-4 h-4" />
                <span>{t('myApplications.actions.cancel')}</span>
              </button>
            </>
          )}

          {isConfirmed && (
            <div className="inline-flex items-center gap-1.5 text-emerald-700 bg-emerald-50 border border-emerald-200 text-sm font-semibold px-4 py-2 rounded-xl">
              <CheckCircle2 className="w-4 h-4 text-emerald-600" />
              <span>{t('myApplications.actions.confirmedNote')}</span>
            </div>
          )}

          {isExpired && (
            <div className="inline-flex items-center gap-1.5 text-rose-700 bg-rose-50 border border-rose-200 text-sm font-medium px-4 py-2 rounded-xl">
              <AlertOctagon className="w-4 h-4 text-rose-600" />
              <span>{t('myApplications.actions.expiredNote')}</span>
            </div>
          )}

          {isCancelled && (
            <div className="inline-flex items-center gap-1.5 text-slate-600 bg-slate-100 border border-slate-200 text-sm font-medium px-4 py-2 rounded-xl">
              <Ban className="w-4 h-4 text-slate-500" />
              <span>{t('myApplications.actions.cancelledNote')}</span>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
