'use client';

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

  const { data: recommendations, isLoading } = useQuery({
    queryKey: ['recommendations', 20],
    queryFn: () => getRecommendations(20),
    enabled: isAuthenticated,
  });

  const { data: coldStart } = useQuery({
    queryKey: ['cold-start', 20],
    queryFn: () => getColdStart(20),
    enabled: !isAuthenticated,
  });

  const tracks = (recommendations || coldStart || []) as Track[];

  return (
    <MainLayout>
      <div className="p-6 max-w-screen-xl mx-auto">
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-8"
        >
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
        </motion.div>

        {isLoading ? (
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
