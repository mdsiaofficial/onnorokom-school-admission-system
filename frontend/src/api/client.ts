import axios, { AxiosError } from 'axios';
import { useStudentStore, STUDENT_ID_REGEX } from '@/stores/studentStore';
import { useClockStore } from '@/stores/clockStore';
import { ProblemDetails, NormalizedError } from './types';

const envApiUrl = import.meta.env.VITE_API_BASE_URL;

export const getApiBaseUrl = (): string => {
  if (typeof window !== 'undefined') {
    // If accessing via an IP or custom hostname other than 'localhost', point API to the same host on port 8080
    if (window.location.hostname && window.location.hostname !== 'localhost') {
      return `${window.location.protocol}//${window.location.hostname}:8080`;
    }
  }
  return envApiUrl || 'http://localhost:8080';
};

export const API_BASE_URL = getApiBaseUrl();

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor: attach X-Student-Id header if valid 17 digits and set dynamic baseURL
apiClient.interceptors.request.use((config) => {
  config.baseURL = getApiBaseUrl();
  const { studentId } = useStudentStore.getState();
  if (studentId && STUDENT_ID_REGEX.test(studentId)) {
    config.headers['X-Student-Id'] = studentId;
  }
  return config;
});

// Response interceptor: track clock skew & normalize errors
apiClient.interceptors.response.use(
  (response) => {
    const dateHeader = response.headers?.['date'];
    if (dateHeader) {
      useClockStore.getState().updateOffsetFromDateHeader(dateHeader);
    }
    return response;
  },
  (error: AxiosError<ProblemDetails>) => {
    const dateHeader = error.response?.headers?.['date'];
    if (dateHeader) {
      useClockStore.getState().updateOffsetFromDateHeader(dateHeader);
    }

    const normalized: NormalizedError = {
      kind: 'unknown',
      message: 'একটি অপ্রত্যাশিত সমস্যা হয়েছে। অনুগ্রহ করে কিছুক্ষণ পর আবার চেষ্টা করুন।',
      raw: error,
    };

    if (error.code === 'ECONNABORTED' || error.message?.includes('timeout')) {
      normalized.kind = 'timeout';
      normalized.message = 'সার্ভারে অনুরোধের সময়সীমা পার হয়ে গেছে।';
    } else if (!error.response) {
      normalized.kind = 'network';
      normalized.message = 'সার্ভারের সাথে সংযোগ করা যাচ্ছে না। আপনার ইন্টারনেট সংযোগ পরীক্ষা করুন।';
    } else {
      normalized.kind = 'api';
      normalized.status = error.response.status;
      const data = error.response.data;
      if (data && typeof data === 'object') {
        normalized.code = data.code;
        normalized.detail = data.detail;
        normalized.message = data.detail || error.message;
      }
    }

    return Promise.reject(normalized);
  }
);
