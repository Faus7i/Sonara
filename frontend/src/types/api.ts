/**
 * API 响应类型 — 与后端 ApiResponse<T> 对应
 */
export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
  errors: string[];
}

/**
 * 曲目基本信息
 */
export interface Track {
  id: string;
  name: string;
  coverImageUrl: string | null;
  durationMs: number;
  popularity: number;
  artistsSummary: string;
  albumName: string | null;
}

/**
 * 推荐结果（含评分和原因）
 */
export interface RecommendationResult extends Track {
  score: number;
  reason: string;
  genres: string[];
}

/**
 * 探索发现结果
 */
export interface DiscoveryResult extends Track {
  discoveryReason: string;
  genres: string[];
}

/**
 * 曲目详情
 */
export interface TrackDetail {
  id: string;
  spotifyTrackId: string;
  name: string;
  durationMs: number;
  popularity: number;
  releaseDate: string;
  coverImageUrl: string | null;
  album: AlbumBrief | null;
  artists: ArtistBrief[];
  audioFeatures: AudioFeatures | null;
}

export interface AlbumBrief {
  id: string;
  name: string;
  coverImageUrl: string | null;
}

export interface ArtistBrief {
  id: string;
  name: string;
}

export interface AudioFeatures {
  danceability: number;
  energy: number;
  valence: number;
  tempo: number;
  acousticness: number;
  instrumentalness: number;
  speechiness: number;
  loudness: number;
  key: number;
  mode: number;
}

/**
 * 播放列表
 */
export interface Playlist {
  id: string;
  name: string;
  description: string;
  coverImageUrl: string | null;
  isPublic: boolean;
  trackCount: number;
  createdAt: string;
}

export interface PlaylistDetail extends Playlist {
  tracks: Track[];
}

/**
 * 用户
 */
export interface User {
  id: string;
  email: string;
  nickname: string;
  avatarUrl: string | null;
}

/**
 * 用户画像
 */
export interface UserProfile {
  userId: string;
  favoriteGenres: string[];
  avgEnergy: number;
  avgDanceability: number;
  avgValence: number;
  avgTempo: number;
  avgAcousticness: number;
  topArtists: string[];
  explorationLevel: number;
  totalPlayCount: number;
}

/**
 * 搜索参数
 */
export type SearchType = 'track' | 'artist' | 'album';

export interface SearchTrack {
  spotifyTrackId: string;
  name: string;
  durationMs: number;
  popularity: number;
  coverImageUrl: string | null;
  albumName: string;
  artistsSummary: string;
}

export interface SearchArtist {
  spotifyArtistId: string;
  name: string;
  genres: string | null;
  imageUrl: string | null;
  popularity: number;
}

export interface SearchAlbum {
  spotifyAlbumId: string;
  name: string;
  releaseDate: string;
  coverImageUrl: string | null;
  albumType: string;
  artistsSummary: string;
}

export interface SearchResult {
  tracks: SearchTrack[];
  artists: SearchArtist[];
  albums: SearchAlbum[];
}

/**
 * 授权
 */
export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  nickname: string;
}
