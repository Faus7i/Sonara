'use client';

import { create } from 'zustand';
import * as authApi from '@/lib/api/auth';
import type { User, LoginRequest, RegisterRequest } from '@/types/api';

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;

  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
  loadFromStorage: () => void;
  fetchProfile: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  token: null,
  isAuthenticated: false,
  isLoading: false,

  login: async (data) => {
    set({ isLoading: true });
    try {
      const result = await authApi.login(data);
      localStorage.setItem('auth-token', result.token);
      localStorage.setItem('auth-user', JSON.stringify(result.user));
      set({ user: result.user, token: result.token, isAuthenticated: true });
    } finally {
      set({ isLoading: false });
    }
  },

  register: async (data) => {
    set({ isLoading: true });
    try {
      const result = await authApi.register(data);
      localStorage.setItem('auth-token', result.token);
      localStorage.setItem('auth-user', JSON.stringify(result.user));
      set({ user: result.user, token: result.token, isAuthenticated: true });
    } finally {
      set({ isLoading: false });
    }
  },

  logout: () => {
    localStorage.removeItem('auth-token');
    localStorage.removeItem('auth-user');
    set({ user: null, token: null, isAuthenticated: false });
  },

  loadFromStorage: () => {
    if (typeof window === 'undefined') return;
    const token = localStorage.getItem('auth-token');
    const userStr = localStorage.getItem('auth-user');
    if (token && userStr) {
      try {
        const user = JSON.parse(userStr) as User;
        set({ token, user, isAuthenticated: true });
      } catch {
        localStorage.removeItem('auth-user');
      }
    }
  },

  fetchProfile: async () => {
    try {
      const user = await authApi.getProfile();
      localStorage.setItem('auth-user', JSON.stringify(user));
      set({ user });
    } catch {
      if (get().isAuthenticated) {
        get().logout();
      }
    }
  },
}));
