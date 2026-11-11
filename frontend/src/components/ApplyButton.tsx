import React from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowRight, RefreshCw } from 'lucide-react';

interface ApplyButtonProps {
  isDisabled: boolean;
  isApplying: boolean;
  onApply: () => void;
  className?: string;
}

export const ApplyButton: React.FC<ApplyButtonProps> = ({
  isDisabled,
  isApplying,
  onApply,
  className = '',
}) => {
  const { t } = useTranslation();

  return (
    <button
      type="button"
      disabled={isDisabled}
      onClick={onApply}
      className={`inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-sm font-semibold transition-all shadow-sm ${
        isDisabled
          ? 'bg-slate-100 text-slate-400 cursor-not-allowed border border-slate-200'
          : 'bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white shadow-emerald-600/20 hover:shadow-md'
      } ${className}`}
    >
      {isApplying ? (
        <>
          <RefreshCw className="w-4 h-4 animate-spin motion-reduce:animate-none" />
          <span>{t('classes.applying')}</span>
        </>
      ) : (
        <>
          <span>{t('classes.applyButton')}</span>
          <ArrowRight className="w-4 h-4" />
        </>
      )}
    </button>
  );
};

interface ApplyReasonProps {
  reason: string;
  className?: string;
}

/**
 * Explains why the apply button is disabled. The text wraps instead of being
 * truncated — a cut-off sentence told the student nothing.
 */
export const ApplyReason: React.FC<ApplyReasonProps> = ({ reason, className = '' }) => {
  if (!reason) return null;
  return (
    <p className={`text-[11px] leading-snug text-slate-500 whitespace-normal ${className}`}>
      {reason}
    </p>
  );
};
