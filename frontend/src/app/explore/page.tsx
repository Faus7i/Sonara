'use client';

import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { RefreshCw, Heart } from 'lucide-react';
import Link from 'next/link';
import { MainLayout } from '@/components/layout/MainLayout';
import { useAuthStore } from '@/store/auth-store';
import { getColdStart } from '@/lib/api/discovery';
import { addFavorite, removeFavorite, checkFavorite } from '@/lib/api/favorites';
import type { DiscoveryResult, Track } from '@/types/api';

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

function TrackCard({
  track,
  index,
  isAuthenticated,
}: {
  track: Track;
  index: number;
  isAuthenticated: boolean;
}) {
  const reason = (track as DiscoveryResult).discoveryReason;
  const isValidTrack = track.id !== EMPTY_GUID;
  const [optimisticLiked, setOptimisticLiked] = useState<boolean | null>(null);
  const [liking, setLiking] = useState(false);

  const { data: liked = false } = useQuery({
    queryKey: ['check-favorite', track.id],
    queryFn: () => checkFavorite(track.id),
    enabled: isAuthenticated && isValidTrack,
    staleTime: 30_000,
  });

  const showLiked = optimisticLiked ?? liked;

  const likeMutation = useMutation({
    mutationFn: () => addFavorite(track.id),
    onSuccess: () => setOptimisticLiked(true),
  });

  const unlikeMutation = useMutation({
    mutationFn: () => removeFavorite(track.id),
    onSuccess: () => setOptimisticLiked(false),
  });

  const handleLike = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (!isAuthenticated || !isValidTrack || liking) return;
    setLiking(true);
    try {
      if (showLiked) {
        await unlikeMutation.mutateAsync();
      } else {
        await likeMutation.mutateAsync();
      }
    } finally {
      setLiking(false);
    }
  };

  const content = (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.05, duration: 0.3 }}
      whileHover={{ scale: 1.02 }}
      className="bg-spotify-card hover:bg-spotify-hover rounded-md p-3 transition-colors group relative"
    >
      <div className="relative mb-3">
        {track.coverImageUrl ? (
          <img
            src={track.coverImageUrl}
            alt={track.name}
            className="w-full aspect-square object-cover rounded shadow-lg"
            loading="lazy"
          />
        ) : (
          <div className="w-full aspect-square bg-spotify-hover rounded flex items-center justify-center text-spotify-subtext text-3xl">
            ♪
          </div>
        )}
        <button
          className="absolute bottom-2 right-2 w-10 h-10 bg-spotify-green rounded-full flex items-center justify-center shadow-xl opacity-0 group-hover:opacity-100 translate-y-2 group-hover:translate-y-0 transition-all duration-200"
          aria-label="播放"
        >
          <svg width="16" height="16" viewBox="0 0 16 16" fill="#121212">
            <path d="M3 1.713a.7.7 0 011.05-.607l10.89 6.288a.7.7 0 010 1.212L4.05 14.894A.7.7 0 013 14.288V1.713z" />
          </svg>
        </button>
        {isAuthenticated && isValidTrack && (
          <button
            onClick={handleLike}
            disabled={liking}
            className={`absolute top-2 right-2 w-8 h-8 rounded-full flex items-center justify-center shadow-lg transition-all duration-200 ${
              showLiked
                ? 'bg-spotify-green text-black'
                : 'bg-black/50 text-white opacity-0 group-hover:opacity-100 hover:scale-110'
            }`}
            aria-label={showLiked ? '取消收藏' : '收藏'}
          >
            <Heart size={14} fill={showLiked ? 'currentColor' : 'none'} />
          </button>
        )}
      </div>
      <h3 className="font-medium text-sm truncate">{track.name}</h3>
      <p className="text-xs text-spotify-subtext truncate mt-1">{track.artistsSummary}</p>
      {reason && (
        <p className="text-xs text-spotify-green/70 mt-1.5 truncate">{reason}</p>
      )}
    </motion.div>
  );

  if (isValidTrack) {
    return <Link href={`/tracks/${track.id}`}>{content}</Link>;
  }
  return content;
}

export default function ExplorePage() {
  const { isAuthenticated } = useAuthStore();
  const [refreshKey, setRefreshKey] = useState(0);

  // 始终使用冷启动端点（无需认证），消除认证状态竞态
  const { data: discovery, isLoading, isFetching, isError, error } = useQuery({
    queryKey: ['explore-cold-start', 20, refreshKey],
    queryFn: () => getColdStart(20),
    staleTime: 0,
    retry: 1,
  });

  // 调试：打印数据状态
  if (typeof window !== 'undefined') {
    console.log('[Explore] isLoading:', isLoading, 'isFetching:', isFetching,
      'isError:', isError, 'data:', discovery, 'error:', error);
  }

  const tracks = (discovery || []) as Track[];
  const isRefreshing = isFetching;

  return (
    <MainLayout>
      <div className="p-6 max-w-screen-xl mx-auto">
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-8 flex items-center justify-between"
        >
          <div>
            <h2 className="text-2xl font-bold">探索新音乐</h2>
            <p className="text-spotify-subtext mt-1 text-sm">
              从 Spotify 曲库随机发现不同风格的音乐
            </p>
          </div>
          <button
            onClick={() => setRefreshKey((k) => k + 1)}
            disabled={isRefreshing}
            className="flex items-center gap-2 px-4 py-2 rounded-full border border-spotify-subtext/30
                       text-spotify-subtext hover:text-white hover:border-white/50
                       disabled:opacity-50 disabled:cursor-not-allowed
                       transition-all duration-200 text-sm font-medium"
          >
            <RefreshCw
              size={16}
              className={isRefreshing ? 'animate-spin' : ''}
            />
            {isRefreshing ? '加载中...' : '换一批'}
          </button>
        </motion.div>

        {isLoading ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
            {Array.from({ length: 20 }).map((_, i) => (
              <div key={i} className="bg-spotify-card rounded-md p-3">
                <div className="w-full aspect-square bg-spotify-hover rounded animate-pulse mb-3" />
                <div className="h-4 bg-spotify-hover rounded animate-pulse mb-2" />
                <div className="h-3 bg-spotify-hover rounded animate-pulse w-2/3" />
              </div>
            ))}
          </div>
        ) : isError ? (
          <div className="text-center py-20">
            <p className="text-lg text-red-400 mb-2">请求出错</p>
            <p className="text-sm text-spotify-subtext">
              {error instanceof Error ? error.message : String(error)}
            </p>
          </div>
        ) : tracks.length > 0 ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
            {tracks.map((track, i) => (
              <TrackCard
                key={track.id || i}
                track={track}
                index={i}
                isAuthenticated={isAuthenticated}
              />
            ))}
          </div>
        ) : (
          <div className="text-center py-20 text-spotify-subtext">
            <p className="text-lg mb-2">暂无探索内容</p>
            <p className="text-sm">请检查后端服务是否正常运行</p>
          </div>
        )}
      </div>
    </MainLayout>
  );
}
