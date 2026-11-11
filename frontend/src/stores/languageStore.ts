import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import i18n from '@/i18n/i18n';

export type SupportedLanguage = 'bn' | 'en';

/**
 * Screen readers pick a pronunciation voice from the document language, and the browser
 * tab title is the accessible name of the page — both must follow the UI language.
 */
const applyDocumentLanguage = (lang: SupportedLanguage) => {
  if (typeof document === 'undefined') return;
  document.documentElement.lang = lang;
  document.title = i18n.t('app.documentTitle', { lng: lang });
};

interface LanguageState {
  language: SupportedLanguage;
  setLanguage: (lang: SupportedLanguage) => void;
  toggleLanguage: () => void;
}

export const useLanguageStore = create<LanguageState>()(
  persist(
    (set, get) => ({
      language: 'bn',
      setLanguage: (lang) => {
        i18n.changeLanguage(lang);
        applyDocumentLanguage(lang);
        set({ language: lang });
      },
      toggleLanguage: () => {
        const next = get().language === 'bn' ? 'en' : 'bn';
        i18n.changeLanguage(next);
        applyDocumentLanguage(next);
        set({ language: next });
      },
    }),
    {
      name: 'school-admission-language',
      onRehydrateStorage: () => (state) => {
        if (state?.language) {
          i18n.changeLanguage(state.language);
          applyDocumentLanguage(state.language);
        }
      },
    }
  )
);
