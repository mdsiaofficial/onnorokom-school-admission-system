import { TFunction } from 'i18next';
import { ClassSummary } from '@/api/types';

export interface ActiveStudentHold {
  status: 'HELD' | 'CONFIRMED';
  className: string;
}

export interface ClassRowState {
  isAlreadyHeld: boolean;
  isAlreadyConfirmed: boolean;
  isSeatsFull: boolean;
  disabledReason: string;
  isDisabled: boolean;
}

/**
 * Single source of truth for the state of one class row.
 *
 * The desktop table and the mobile card list used to carry their own copy of this
 * logic, which is how they drifted apart.
 *
 * This is deliberately a plain function and not a hook: it is called once per class
 * inside a list render, and the number of classes changes between renders, so a hook
 * here would break the rules of hooks.
 */
export const getClassRowState = (
  t: TFunction,
  cls: ClassSummary,
  existingStatus: 'HELD' | 'CONFIRMED' | undefined,
  activeStudentHold: ActiveStudentHold | null,
  isStudentIdValid: boolean,
  isApplyingThis: boolean
): ClassRowState => {
  const isAlreadyHeld = existingStatus === 'HELD';
  const isAlreadyConfirmed = existingStatus === 'CONFIRMED';
  const isSeatsFull = cls.availableSeats <= 0;

  let disabledReason = '';
  if (!isStudentIdValid) {
    disabledReason = t('classes.reasons.enterStudentId');
  } else if (isAlreadyConfirmed) {
    disabledReason = t('classes.reasons.alreadyConfirmed');
  } else if (isAlreadyHeld) {
    disabledReason = t('classes.reasons.alreadyHeld');
  } else if (activeStudentHold) {
    disabledReason =
      activeStudentHold.status === 'CONFIRMED'
        ? t('classes.reasons.confirmedInOther', { className: activeStudentHold.className })
        : t('classes.reasons.pendingInOther', { className: activeStudentHold.className });
  } else if (isSeatsFull) {
    disabledReason = t('classes.reasons.noSeats');
  }

  return {
    isAlreadyHeld,
    isAlreadyConfirmed,
    isSeatsFull,
    disabledReason,
    isDisabled: Boolean(disabledReason) || isApplyingThis,
  };
};
