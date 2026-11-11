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

  // Unclassified failures fall back to the translated generic message so the UI never
  // shows a language the user did not pick.
  if (norm.kind === 'unknown') {
    return i18n.t('errors.GENERIC');
  }

  if (norm.code) {
    const key = `errors.${norm.code}`;
    if (i18n.exists(key)) {
      return i18n.t(key);
    }
  }

  // The API sends a precise explanation even for codes we have no translation for
  // (e.g. "Cannot reduce total seats to 5 because 60 seats are already held").
  // Prefer it over the generic message instead of throwing that detail away.
  if (norm.detail) {
    return norm.detail;
  }

  if (norm.message && norm.kind !== 'api') {
    return norm.message;
  }

  return i18n.t('errors.GENERIC');
};
