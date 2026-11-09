import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import i18n from '@/i18n/i18n';

export type SupportedLanguage = 'bn' | 'en';

interface LanguageState {
  language: SupportedLanguage;
  setLanguage: (lang: SupportedLanguage) => void;
  toggleLanguage: () => void;
}

export const useLanguageStore = create<LanguageState>()(
  persist(
    (set, get) => ({
      language: 'bn',
      setLanguage: (lang: SupportedLanguage) => {
        i18n.changeLanguage(lang);
        set({ language: lang });
      },
      toggleLanguage: () => {
        const next = get().language === 'bn' ? 'en' : 'bn';
        i18n.changeLanguage(next);
        set({ language: next });
      },
    }),
    {
      name: 'school-admission-language',
      onRehydrateStorage: () => (state) => {
        if (state?.language) {
          i18n.changeLanguage(state.language);
        }
      },
    }
  )
);
