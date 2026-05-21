import apiClient from '../api-client';
import type { AuthResponse, LoginRequest, RegisterRequest, User } from '@/types/api';

export async function login(data: LoginRequest): Promise<AuthResponse> {
  return apiClient.post('/identity/login', data);
}

export async function register(data: RegisterRequest): Promise<AuthResponse> {
  return apiClient.post('/identity/register', data);
}

export async function getProfile(): Promise<User> {
  return apiClient.get('/identity/profile');
}

export async function updateProfile(data: { nickname?: string; avatarUrl?: string }): Promise<User> {
  return apiClient.put('/identity/profile', data);
}
