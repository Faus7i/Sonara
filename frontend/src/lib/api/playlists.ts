import apiClient from '../api-client';
import type { Playlist, PlaylistDetail, Track } from '@/types/api';

export async function getPlaylists(): Promise<Playlist[]> {
  return apiClient.get('/playlists');
}

export async function getPlaylistDetail(id: string): Promise<PlaylistDetail> {
  return apiClient.get(`/playlists/${id}`);
}

export async function createPlaylist(name: string, description?: string): Promise<Playlist> {
  return apiClient.post('/playlists', { name, description });
}

export async function deletePlaylist(id: string): Promise<void> {
  return apiClient.delete(`/playlists/${id}`);
}

export async function addTrackToPlaylist(playlistId: string, trackId: string): Promise<void> {
  return apiClient.post(`/playlists/${playlistId}/tracks`, { trackId });
}

export async function removeTrackFromPlaylist(playlistId: string, trackId: string): Promise<void> {
  return apiClient.delete(`/playlists/${playlistId}/tracks/${trackId}`);
}
