import apiClient from '../api-client';
import type { TrackDetail } from '@/types/api';

export async function getTrackDetail(trackId: string): Promise<TrackDetail> {
  return apiClient.get(`/catalog/tracks/${trackId}`);
}

export async function getArtistDetail(artistId: string): Promise<unknown> {
  return apiClient.get(`/catalog/artists/${artistId}`);
}

export async function getAlbumDetail(albumId: string): Promise<unknown> {
  return apiClient.get(`/catalog/albums/${albumId}`);
}

export async function importTrack(spotifyTrackId: string): Promise<TrackDetail> {
  return apiClient.post('/catalog/tracks/import', { spotifyTrackId });
}
