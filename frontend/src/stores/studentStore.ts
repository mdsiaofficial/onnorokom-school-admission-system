import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface StudentState {
  studentId: string;
  setStudentId: (rawId: string) => void;
  clearStudentId: () => void;
  isValid: boolean;
}

export const STUDENT_ID_REGEX = /^[0-9]{17}$/;

export const useStudentStore = create<StudentState>()(
  persist(
    (set) => ({
      studentId: '',
      isValid: false,
      setStudentId: (rawId: string) => {
        // Strip non-digits and cap at 17 characters
        const cleaned = rawId.replace(/\D/g, '').slice(0, 17);
        set({
          studentId: cleaned,
          isValid: STUDENT_ID_REGEX.test(cleaned),
        });
      },
      clearStudentId: () => {
        set({ studentId: '', isValid: false });
      },
    }),
    {
      name: 'school-admission-student-id',
      // Re-derive isValid on rehydration
      onRehydrateStorage: () => (state) => {
        if (state) {
          state.isValid = STUDENT_ID_REGEX.test(state.studentId);
        }
      },
    }
  )
);
