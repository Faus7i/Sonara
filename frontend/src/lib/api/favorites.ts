import apiClient from '../api-client';
import type { Track } from '@/types/api';

export async function getFavorites(page = 1, pageSize = 20): Promise<Track[]> {
  return apiClient.get('/favorites', { params: { page, pageSize } });
}

export async function addFavorite(trackId: string): Promise<void> {
  return apiClient.post(`/favorites/tracks/${trackId}`);
}

export async function removeFavorite(trackId: string): Promise<void> {
  return apiClient.delete(`/favorites/tracks/${trackId}`);
}

export async function checkFavorite(trackId: string): Promise<boolean> {
  return apiClient.get(`/favorites/check/${trackId}`);
}
