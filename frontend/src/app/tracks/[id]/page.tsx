'use client';

import { useParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { MainLayout } from '@/components/layout/MainLayout';
import { getTrackDetail } from '@/lib/api/catalog';
import { getSimilarTracks } from '@/lib/api/recommendations';
import type { RecommendationResult } from '@/types/api';

function AudioFeatureBar({ label, value, unit = '' }: { label: string; value: number; unit?: string }) {
  const pct = Math.round(value * 100);
  return (
    <div className="flex items-center gap-3">
      <span className="w-32 text-xs text-spotify-subtext text-right">{label}</span>
      <div className="flex-1 h-2 bg-spotify-hover rounded-full overflow-hidden">
        <motion.div
          initial={{ width: 0 }}
          animate={{ width: `${pct}%` }}
          transition={{ duration: 0.8, delay: 0.2 }}
          className="h-full bg-spotify-green rounded-full"
        />
      </div>
      <span className="text-xs text-spotify-subtext w-14">
        {unit}{value.toFixed(2)}
      </span>
    </div>
  );
}

/** Spotify Pitch Class Notation (0=C, 1=C#, ..., 11=B) 转音名，-1 表示无法检测 */
function keyToName(key: number): string {
  const notes = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'];
  return key >= 0 && key <= 11 ? notes[key] : '未知';
}

/** 响度 dB 值归一化到 [0,1]，用于进度条可视化。Spotify 响度典型范围 -60~0 dB */
function normalizeLoudness(db: number): number {
  return Math.max(0, Math.min(1, (db + 60) / 60));
}

/** 调式文本：1=大调，0=小调，-1/其他=无法检测 */
function modeToLabel(mode: number): string {
  if (mode === 1) return '大调';
  if (mode === 0) return '小调';
  return '未知调式';
}

export default function TrackDetailPage() {
  const params = useParams();
  const trackId = params.id as string;

  const { data: track, isLoading } = useQuery({
    queryKey: ['track', trackId],
    queryFn: () => getTrackDetail(trackId),
    enabled: !!trackId,
  });

  const { data: similar } = useQuery({
    queryKey: ['similar-tracks', trackId],
    queryFn: () => getSimilarTracks(trackId, 6),
    enabled: !!trackId,
  });

  if (isLoading) {
    return (
      <MainLayout>
        <div className="p-6 animate-pulse">
          <div className="flex gap-6 mb-8">
            <div className="w-48 h-48 bg-spotify-card rounded-lg" />
            <div className="flex-1 space-y-3">
              <div className="h-6 bg-spotify-card rounded w-1/3" />
              <div className="h-4 bg-spotify-card rounded w-1/4" />
              <div className="h-4 bg-spotify-card rounded w-1/2" />
            </div>
          </div>
        </div>
      </MainLayout>
    );
  }

  if (!track) {
    return (
      <MainLayout>
        <div className="text-center py-20 text-spotify-subtext">
          <p className="text-lg">曲目不存在</p>
        </div>
      </MainLayout>
    );
  }

  const af = track.audioFeatures;
  const minutes = Math.floor(track.durationMs / 60000);
  const seconds = Math.floor((track.durationMs % 60000) / 1000);

  return (
    <MainLayout>
      <div className="p-6 max-w-screen-xl mx-auto">
        {/* 曲目头部 */}
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="flex flex-col sm:flex-row gap-6 mb-10"
        >
          {track.coverImageUrl ? (
            <img
              src={track.coverImageUrl}
              alt={track.name}
              className="w-48 h-48 rounded-lg shadow-2xl object-cover"
            />
          ) : (
            <div className="w-48 h-48 bg-spotify-card rounded-lg flex items-center justify-center text-spotify-subtext text-5xl">
              ♪
            </div>
          )}
          <div className="flex flex-col justify-end">
            <p className="text-xs text-spotify-subtext uppercase tracking-wider mb-1">曲目</p>
            <h1 className="text-3xl font-bold mb-2">{track.name}</h1>
            <p className="text-sm text-spotify-subtext">
              {track.artists.map((a) => a.name).join(', ')}
              {track.album && ` · ${track.album.name}`}
            </p>
            <p className="text-xs text-spotify-subtext mt-1">
              {minutes}:{seconds.toString().padStart(2, '0')}
              {track.releaseDate && ` · ${track.releaseDate}`}
            </p>
          </div>
        </motion.div>

        {/* 音频特征分析 */}
        {af && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.2 }}
            className="bg-spotify-dark rounded-lg p-6 mb-8"
          >
            <h2 className="text-lg font-semibold mb-4">音频特征分析</h2>
            <div className="space-y-3 max-w-lg">
              <AudioFeatureBar label="舞蹈性" value={af.danceability} />
              <AudioFeatureBar label="能量" value={af.energy} />
              <AudioFeatureBar label="情绪效价" value={af.valence} />
              <AudioFeatureBar label="原声度" value={af.acousticness} />
              <AudioFeatureBar label="器乐性" value={af.instrumentalness} />
              <AudioFeatureBar label="语音度" value={af.speechiness} />
              <AudioFeatureBar label="节奏" value={af.tempo} unit="BPM " />
              {/* 响度：进度条用归一化 [0,1] 值驱动，文本显示原始 dB 值 */}
              <div className="flex items-center gap-3">
                <span className="w-32 text-xs text-spotify-subtext text-right">响度</span>
                <div className="flex-1 h-2 bg-spotify-hover rounded-full overflow-hidden">
                  <motion.div
                    initial={{ width: 0 }}
                    animate={{ width: `${Math.round(normalizeLoudness(af.loudness) * 100)}%` }}
                    transition={{ duration: 0.8, delay: 0.2 }}
                    className="h-full bg-spotify-green rounded-full"
                  />
                </div>
                <span className="text-xs text-spotify-subtext w-14">{af.loudness.toFixed(1)} dB</span>
              </div>
              {/* 调性与调式：非数值维度，用纯文本展示 */}
              <div className="flex items-center gap-3">
                <span className="w-32 text-xs text-spotify-subtext text-right">调性</span>
                <span className="text-xs text-white/80">
                  {keyToName(af.key)}
                  <span className="text-spotify-subtext ml-1">({modeToLabel(af.mode)})</span>
                </span>
              </div>
            </div>
          </motion.div>
        )}

        {/* 相似曲目 */}
        {similar && similar.length > 0 && (
          <div>
            <h2 className="text-lg font-semibold mb-4">相似曲目</h2>
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-6 gap-3">
              {similar.map((r: RecommendationResult, i: number) => (
                <motion.div
                  key={r.id}
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.3 + i * 0.05 }}
                  className="bg-spotify-card hover:bg-spotify-hover rounded-md p-2 cursor-pointer transition-colors"
                >
                  {r.coverImageUrl ? (
                    <img src={r.coverImageUrl} alt={r.name} className="w-full aspect-square object-cover rounded mb-2" loading="lazy" />
                  ) : (
                    <div className="w-full aspect-square bg-spotify-hover rounded mb-2 flex items-center justify-center text-2xl">♪</div>
                  )}
                  <p className="text-xs font-medium truncate">{r.name}</p>
                  <p className="text-xs text-spotify-subtext truncate">{r.artistsSummary}</p>
                  <p className="text-xs text-spotify-green/70 mt-1 truncate">{r.reason}</p>
                </motion.div>
              ))}
            </div>
          </div>
        )}
      </div>
    </MainLayout>
  );
}
