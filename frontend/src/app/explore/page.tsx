'use client';

import { useQuery } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { MainLayout } from '@/components/layout/MainLayout';
import { useAuthStore } from '@/store/auth-store';
import { getDiscovery } from '@/lib/api/discovery';
import { getColdStart } from '@/lib/api/discovery';
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

  const { data: discovery, isLoading } = useQuery({
    queryKey: ['discovery', 20],
    queryFn: () => getDiscovery(20),
    enabled: isAuthenticated,
  });

  const { data: coldStart, isLoading: coldLoading } = useQuery({
    queryKey: ['cold-start-explore', 20],
    queryFn: () => getColdStart(20),
    enabled: !isAuthenticated,
  });

  const tracks = (discovery || coldStart || []) as Track[];
  const loading = isLoading || coldLoading;

  return (
    <MainLayout>
      <div className="p-6 max-w-screen-xl mx-auto">
        <motion.div initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} className="mb-8">
          <h2 className="text-2xl font-bold">探索新音乐</h2>
          <p className="text-spotify-subtext mt-1 text-sm">发现不同流派和风格的音乐</p>
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
