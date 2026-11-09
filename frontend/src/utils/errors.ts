import i18n from '@/i18n/i18n';
import { NormalizedError } from '@/api/types';

export const getLocalizedErrorMessage = (error: unknown): string => {
  if (!error) return i18n.t('errors.GENERIC');

  const norm = error as NormalizedError;

  if (norm.kind === 'network') {
    return i18n.t('errors.NETWORK_ERROR');
  }

  if (norm.kind === 'timeout') {
    return i18n.t('errors.TIMEOUT');
  }

  if (norm.code) {
    const key = `errors.${norm.code}`;
    if (i18n.exists(key)) {
      return i18n.t(key);
    }
  }

  if (norm.message && norm.kind !== 'api') {
    return norm.message;
  }

  return i18n.t('errors.GENERIC');
};
