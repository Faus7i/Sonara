import apiClient from '../api-client';
import type { SearchResult, SearchType } from '@/types/api';

export async function search(
  query: string,
  type: SearchType = 'track',
  limit = 20
): Promise<SearchResult> {
  return apiClient.get('/search', { params: { q: query, type, limit } });
}
