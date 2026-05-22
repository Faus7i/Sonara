'use client';

import { useQuery, useMutation } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { RefreshCw, Database } from 'lucide-react';
import { MainLayout } from '@/components/layout/MainLayout';
import { useAuthStore } from '@/store/auth-store';
import { getDiscovery, getColdStart, seedTracks } from '@/lib/api/discovery';
import type { DiscoveryResult, Track } from '@/types/api';

function TrackCard({ track, index }: { track: Track; index: number }) {
  const reason = (track as DiscoveryResult).discoveryReason;

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.05, duration: 0.3 }}
      className="bg-spotify-card hover:bg-spotify-hover rounded-md p-3 cursor-pointer transition-colors"
    >
      <div className="mb-3">
        {track.coverImageUrl ? (
          <img src={track.coverImageUrl} alt={track.name} className="w-full aspect-square object-cover rounded shadow-lg" loading="lazy" />
        ) : (
          <div className="w-full aspect-square bg-spotify-hover rounded flex items-center justify-center text-spotify-subtext text-3xl">♪</div>
        )}
      </div>
      <h3 className="font-medium text-sm truncate">{track.name}</h3>
      <p className="text-xs text-spotify-subtext truncate mt-1">{track.artistsSummary}</p>
      {reason && <p className="text-xs text-spotify-blue/70 mt-1.5 truncate">{reason}</p>}
    </motion.div>
  );
}

export default function ExplorePage() {
  const { isAuthenticated } = useAuthStore();

  const { data: discovery, isLoading, isFetching: isFetchingDiscovery, refetch: refetchDiscovery } = useQuery({
    queryKey: ['discovery', 20],
    queryFn: () => getDiscovery(20),
    enabled: isAuthenticated,
  });

  const { data: coldStart, isLoading: coldLoading, isFetching: isFetchingCold, refetch: refetchColdStart } = useQuery({
    queryKey: ['cold-start-explore', 20],
    queryFn: () => getColdStart(20),
    enabled: !isAuthenticated,
  });

  const seedMutation = useMutation({
    mutationFn: seedTracks,
    onSuccess: () => {
      if (isAuthenticated) {
        refetchDiscovery();
      } else {
        refetchColdStart();
      }
    },
  });

  const tracks = (discovery || coldStart || []) as Track[];
  const loading = isLoading || coldLoading;
  const isRefreshing = isFetchingDiscovery || isFetchingCold;

  return (
    <MainLayout>
      <div className="p-6 max-w-screen-xl mx-auto">
        <motion.div initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} className="mb-8">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-2xl font-bold">探索新音乐</h2>
              <p className="text-spotify-subtext mt-1 text-sm">发现不同流派和风格的音乐</p>
            </div>
            <div className="flex items-center gap-3">
              <button
                onClick={() => seedMutation.mutate()}
                disabled={seedMutation.isPending}
                className="flex items-center gap-2 px-4 py-2 text-sm bg-spotify-card hover:bg-spotify-hover rounded-full transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                title="生成种子数据（约 140 首曲目，覆盖 23 个流派）"
              >
                <Database size={16} className={seedMutation.isPending ? 'animate-spin' : ''} />
                {seedMutation.isPending ? '生成中...' : '生成种子数据'}
              </button>
              <button
                onClick={() => {
                  if (isAuthenticated) refetchDiscovery();
                  else refetchColdStart();
                }}
                className="flex items-center gap-2 px-4 py-2 text-sm bg-spotify-green text-black font-semibold rounded-full hover:bg-spotify-green/80 transition-colors"
                title="刷新推荐结果"
              >
                <RefreshCw size={16} className={isRefreshing ? 'animate-spin' : ''} />
                刷新推荐
              </button>
            </div>
          </div>
        </motion.div>

        {loading ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
            {Array.from({ length: 20 }).map((_, i) => (
              <div key={i} className="bg-spotify-card rounded-md p-3">
                <div className="w-full aspect-square bg-spotify-hover rounded animate-pulse mb-3" />
                <div className="h-4 bg-spotify-hover rounded animate-pulse mb-2" />
                <div className="h-3 bg-spotify-hover rounded animate-pulse w-2/3" />
              </div>
            ))}
          </div>
        ) : tracks.length > 0 ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
            {tracks.map((track, i) => (
              <TrackCard key={track.id} track={track} index={i} />
            ))}
          </div>
        ) : (
          <div className="text-center py-20 text-spotify-subtext">
            <p className="text-lg mb-2">暂无探索内容</p>
            <p className="text-sm">请先在搜索页面导入一些曲目</p>
          </div>
        )}
      </div>
    </MainLayout>
  );
}
