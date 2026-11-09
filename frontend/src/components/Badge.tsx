import React from 'react';
import { useTranslation } from 'react-i18next';
import { HoldStatus } from '@/api/types';
import { Clock, CheckCircle2, AlertOctagon, Ban } from 'lucide-react';

interface BadgeProps {
  status: HoldStatus;
}

export const StatusBadge: React.FC<BadgeProps> = ({ status }) => {
  const { t } = useTranslation();

  switch (status) {
    case 'HELD':
      return (
        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200">
          <Clock className="w-3.5 h-3.5" />
          {t('myApplications.status.HELD')}
        </span>
      );
    case 'CONFIRMED':
      return (
        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
          <CheckCircle2 className="w-3.5 h-3.5" />
          {t('myApplications.status.CONFIRMED')}
        </span>
      );
    case 'EXPIRED':
      return (
        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-rose-50 text-rose-700 border border-rose-200">
          <AlertOctagon className="w-3.5 h-3.5" />
          {t('myApplications.status.EXPIRED')}
        </span>
      );
    case 'CANCELLED':
      return (
        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-slate-100 text-slate-600 border border-slate-200">
          <Ban className="w-3.5 h-3.5" />
          {t('myApplications.status.CANCELLED')}
        </span>
      );
    default:
      return (
        <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-slate-100 text-slate-700">
          {status}
        </span>
      );
  }
};
