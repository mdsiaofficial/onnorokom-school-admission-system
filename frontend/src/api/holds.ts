import { apiClient } from './client';
import { GetHoldResponse, ConfirmHoldResponse, StudentHold } from './types';

export const getHoldById = async (holdId: string): Promise<GetHoldResponse> => {
  const response = await apiClient.get<GetHoldResponse>(`/holds/${encodeURIComponent(holdId)}`);
  return response.data;
};

export const confirmHold = async (holdId: string): Promise<ConfirmHoldResponse> => {
  const response = await apiClient.post<ConfirmHoldResponse>(
    `/holds/${encodeURIComponent(holdId)}/confirm`
  );
  return response.data;
};

export const cancelHold = async (holdId: string): Promise<void> => {
  await apiClient.delete(`/holds/${encodeURIComponent(holdId)}`);
};

export const getMyHolds = async (): Promise<StudentHold[]> => {
  const response = await apiClient.get<StudentHold[]>('/me/holds');
  return response.data;
};
