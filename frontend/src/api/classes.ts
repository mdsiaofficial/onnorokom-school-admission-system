import { apiClient } from './client';
import { ClassSummary, CreateClassRequest, HoldResponse } from './types';

export const getClasses = async (): Promise<ClassSummary[]> => {
  const response = await apiClient.get<ClassSummary[]>('/classes');
  return response.data;
};

export const getClassById = async (classId: string): Promise<ClassSummary> => {
  const response = await apiClient.get<ClassSummary>(`/classes/${encodeURIComponent(classId)}`);
  return response.data;
};

export const createClass = async (data: CreateClassRequest): Promise<ClassSummary> => {
  const response = await apiClient.post<ClassSummary>('/classes', data);
  return response.data;
};

export const updateClass = async (classId: string, data: { name: string; totalSeats: number }): Promise<ClassSummary> => {
  const response = await apiClient.put<ClassSummary>(`/classes/${encodeURIComponent(classId)}`, data);
  return response.data;
};

export const deleteClass = async (classId: string): Promise<void> => {
  await apiClient.delete(`/classes/${encodeURIComponent(classId)}`);
};

export const applyForSeat = async (classId: string): Promise<HoldResponse> => {
  const response = await apiClient.post<HoldResponse>(
    `/classes/${encodeURIComponent(classId)}/holds`
  );
  return response.data;
};
