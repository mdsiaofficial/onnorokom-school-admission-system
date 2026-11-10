export type HoldStatus = 'HELD' | 'CONFIRMED' | 'EXPIRED' | 'CANCELLED';

export interface ClassSummary {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}

export interface CreateClassRequest {
  name: string;
  totalSeats: number;
}

export interface UpdateClassRequest {
  name: string;
  totalSeats: number;
}

export interface HoldResponse {
  holdId: string;
  classId: string;
  studentId: string;
  status: HoldStatus;
  expiresAt: string;
}

export interface GetHoldResponse {
  holdId: string;
  classId: string;
  status: HoldStatus;
  expiresAt: string;
}

export interface ConfirmHoldResponse {
  holdId: string;
  status: 'CONFIRMED';
}

export interface StudentHold {
  holdId: string;
  classId: string;
  className: string;
  status: HoldStatus;
  expiresAt: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  code?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export interface NormalizedError {
  kind: 'api' | 'network' | 'timeout' | 'unknown';
  code?: string;
  status?: number;
  message: string;
  detail?: string;
  raw?: unknown;
}
