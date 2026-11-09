import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface AuthState {
  isTeacher: boolean;
  teacherName: string;
  loginAsTeacher: () => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      isTeacher: false,
      teacherName: 'মোঃ রফিকুল ইসলাম (প্রধান শিক্ষক)',
      loginAsTeacher: () => set({ isTeacher: true }),
      logout: () => set({ isTeacher: false }),
    }),
    {
      name: 'school-admission-auth',
    }
  )
);
