'use client';

import { useState, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { MainLayout } from '@/components/layout/MainLayout';
import { useAuthStore } from '@/store/auth-store';
import { getRecommendations } from '@/lib/api/recommendations';
import { getColdStart } from '@/lib/api/discovery';
import type { RecommendationResult, DiscoveryResult, Track } from '@/types/api';

function TrackCard({ track, index }: { track: Track; index: number }) {
  const reason = (track as RecommendationResult).reason || (track as DiscoveryResult).discoveryReason;

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.05, duration: 0.3 }}
      whileHover={{ scale: 1.02 }}
      className="bg-spotify-card hover:bg-spotify-hover rounded-md p-3 cursor-pointer transition-colors group"
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
      </div>
      <h3 className="font-medium text-sm truncate">{track.name}</h3>
      <p className="text-xs text-spotify-subtext truncate mt-1">{track.artistsSummary}</p>
      {reason && (
        <p className="text-xs text-spotify-green/70 mt-1.5 truncate">{reason}</p>
      )}
    </motion.div>
  );
}

function SkeletonGrid({ count = 20 }: { count?: number }) {
  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="bg-spotify-card rounded-md p-3">
          <div className="w-full aspect-square bg-spotify-hover rounded animate-pulse mb-3" />
          <div className="h-4 bg-spotify-hover rounded animate-pulse mb-2" />
          <div className="h-3 bg-spotify-hover rounded animate-pulse w-2/3" />
        </div>
      ))}
    </div>
  );
}

export default function HomePage() {
  const { isAuthenticated, user } = useAuthStore();
  const [refreshCounter, setRefreshCounter] = useState(0);

  const isForceRefresh = refreshCounter > 0;

  const { data: recommendations, isLoading, isFetching } = useQuery({
    queryKey: ['recommendations', 20, refreshCounter],
    queryFn: () => getRecommendations(20, isForceRefresh),
    enabled: isAuthenticated,
    staleTime: 0, // 每次页面切换都重新请求
  });

  const { data: coldStart, isLoading: coldLoading, isFetching: coldFetching } = useQuery({
    queryKey: ['cold-start', 20, refreshCounter],
    queryFn: () => getColdStart(20),
    enabled: !isAuthenticated,
    staleTime: 0,
  });

  const tracks = (recommendations || coldStart || []) as Track[];
  const loading = isAuthenticated ? isLoading : coldLoading;
  const fetching = isAuthenticated ? isFetching : coldFetching;

  const handleRefresh = useCallback(() => {
    setRefreshCounter(c => c + 1);
  }, []);

  return (
    <MainLayout>
      <div className="p-6 max-w-screen-xl mx-auto">
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-8 flex items-center justify-between"
        >
          <div>
            <h2 className="text-2xl font-bold">
              {isAuthenticated && user
                ? `${user.nickname || '你好'}，为你推荐`
                : '热门推荐'}
            </h2>
            <p className="text-spotify-subtext mt-1 text-sm">
              {isAuthenticated
                ? '基于你的音乐品味，精心挑选'
                : '登录后获取个性化推荐'}
            </p>
          </div>
          <button
            onClick={handleRefresh}
            disabled={fetching}
            className="flex items-center gap-2 px-4 py-2 rounded-full border border-spotify-subtext/30
                       text-spotify-subtext hover:text-white hover:border-white/50
                       disabled:opacity-50 disabled:cursor-not-allowed
                       transition-all duration-200 text-sm font-medium"
          >
            <svg
              className={`w-4 h-4 ${fetching ? 'animate-spin' : ''}`}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
              />
            </svg>
            {fetching ? '加载中...' : '换一批'}
          </button>
        </motion.div>

        {loading ? (
          <SkeletonGrid />
        ) : tracks.length > 0 ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
            {tracks.map((track, i) => (
              <TrackCard key={track.id} track={track} index={i} />
            ))}
          </div>
        ) : (
          <div className="text-center py-20 text-spotify-subtext">
            <p className="text-lg mb-2">还没有推荐内容</p>
            <p className="text-sm">
              请先在搜索页面导入一些曲目，或运行种子数据生成
            </p>
          </div>
        )}
      </div>
    </MainLayout>
  );
}
