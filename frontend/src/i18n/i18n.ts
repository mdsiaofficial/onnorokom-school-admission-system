import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import bnTranslation from './bn.json';
import enTranslation from './en.json';

const resources = {
  bn: {
    translation: bnTranslation,
  },
  en: {
    translation: enTranslation,
  },
};

// Check if localStorage has stored language preference
let initialLanguage = 'bn';
try {
  const stored = localStorage.getItem('school-admission-language');
  if (stored) {
    const parsed = JSON.parse(stored);
    if (parsed?.state?.language) {
      initialLanguage = parsed.state.language;
    }
  }
} catch {
  // Use default
}

i18n.use(initReactI18next).init({
  resources,
  lng: initialLanguage,
  fallbackLng: 'bn',
  interpolation: {
    escapeValue: false, // React already escapes values
  },
});

export default i18n;
