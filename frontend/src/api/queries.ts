import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getClasses, createClass, updateClass, deleteClass, applyForSeat } from './classes';
import { getMyHolds, confirmHold, cancelHold } from './holds';
import { useStudentStore } from '@/stores/studentStore';
import { CreateClassRequest, UpdateClassRequest } from './types';

export const useClassesQuery = () => {
  return useQuery({
    queryKey: ['classes'],
    queryFn: getClasses,
    refetchInterval: 3000, // Poll every ~3s per spec
    refetchIntervalInBackground: false,
    staleTime: 2000,
  });
};

export const useMyHoldsQuery = (enabled = true) => {
  const { studentId, isValid } = useStudentStore();

  return useQuery({
    queryKey: ['my-holds', studentId],
    queryFn: getMyHolds,
    enabled: enabled && isValid && Boolean(studentId),
    refetchInterval: 5000, // Poll every ~5s
    refetchIntervalInBackground: false,
    staleTime: 3000,
  });
};

export const useApplySeatMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (classId: string) => applyForSeat(classId),
    onSettled: () => {
      // Invalidate both classes and my-holds on success or conflict error
      queryClient.invalidateQueries({ queryKey: ['classes'] });
      queryClient.invalidateQueries({ queryKey: ['my-holds'] });
    },
  });
};

export const useConfirmHoldMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (holdId: string) => confirmHold(holdId),
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
      queryClient.invalidateQueries({ queryKey: ['my-holds'] });
    },
  });
};

export const useCancelHoldMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (holdId: string) => cancelHold(holdId),
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
      queryClient.invalidateQueries({ queryKey: ['my-holds'] });
    },
  });
};

export const useCreateClassMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateClassRequest) => createClass(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
    },
  });
};

export const useUpdateClassMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ classId, data }: { classId: string; data: UpdateClassRequest }) =>
      updateClass(classId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
    },
  });
};

export const useDeleteClassMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (classId: string) => deleteClass(classId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
    },
  });
};
