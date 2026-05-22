import apiClient from '../api-client';
import type { SearchResult, SearchType } from '@/types/api';

export async function search(
  query: string,
  type: SearchType = 'track',
  limit = 20
): Promise<SearchResult> {
  return apiClient.get('/search', { params: { q: query, type, limit } });
}

// 后端单一类型搜索时只返回对应数组，前端统一处理
export async function searchTracks(query: string, limit = 20): Promise<SearchResult> {
  return search(query, 'track', limit);
}

export async function searchArtists(query: string, limit = 20): Promise<SearchResult> {
  return search(query, 'artist', limit);
}

export async function searchAlbums(query: string, limit = 20): Promise<SearchResult> {
  return search(query, 'album', limit);
}
