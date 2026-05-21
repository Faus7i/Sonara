import apiClient from '../api-client';
import type { DiscoveryResult } from '@/types/api';

export async function getDiscovery(limit = 20): Promise<DiscoveryResult[]> {
  return apiClient.get('/discovery', { params: { limit } });
}

export async function getColdStart(limit = 20): Promise<DiscoveryResult[]> {
  return apiClient.get('/discovery/cold-start', { params: { limit } });
}
