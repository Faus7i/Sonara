import apiClient from '../api-client';
import type { RecommendationResult } from '@/types/api';

export async function getRecommendations(limit = 20, forceRefresh = false): Promise<RecommendationResult[]> {
  return apiClient.get('/recommendations', { params: { limit, forceRefresh } });
}

export async function getSimilarTracks(trackId: string, limit = 10): Promise<RecommendationResult[]> {
  return apiClient.get(`/recommendations/similar/${trackId}`, { params: { limit } });
}

export async function seedAudioFeatures(): Promise<{ generatedCount: number }> {
  return apiClient.post('/recommendations/seed');
}
