'use client';

import { useQuery } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { ListMusic } from 'lucide-react';
import { MainLayout } from '@/components/layout/MainLayout';
import { getPlaylists } from '@/lib/api/playlists';
import type { Playlist } from '@/types/api';

export default function PlaylistsPage() {
  const { data: playlists, isLoading } = useQuery({
    queryKey: ['playlists'],
    queryFn: () => getPlaylists(),
  });

  return (
    <MainLayout>
      <div className="p-6 max-w-screen-xl mx-auto">
        <motion.div initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} className="mb-8">
          <h2 className="text-2xl font-bold flex items-center gap-2">
            <ListMusic size={24} className="text-spotify-green" />
            我的歌单
          </h2>
          <p className="text-spotify-subtext mt-1 text-sm">
            {playlists ? `${playlists.length} 个歌单` : '加载中...'}
          </p>
        </motion.div>

        {isLoading ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <div key={i} className="bg-spotify-card rounded-md p-3">
                <div className="w-full aspect-square bg-spotify-hover rounded animate-pulse mb-3" />
                <div className="h-4 bg-spotify-hover rounded animate-pulse mb-2" />
                <div className="h-3 bg-spotify-hover rounded animate-pulse w-1/2" />
              </div>
            ))}
          </div>
        ) : playlists && playlists.length > 0 ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
            {playlists.map((pl: Playlist, i: number) => (
              <motion.div
                key={pl.id}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.05 }}
                whileHover={{ scale: 1.02 }}
                className="bg-spotify-card hover:bg-spotify-hover rounded-md p-3 cursor-pointer transition-colors"
              >
                {pl.coverImageUrl ? (
                  <img src={pl.coverImageUrl} alt={pl.name} className="w-full aspect-square object-cover rounded shadow-lg mb-3" loading="lazy" />
                ) : (
                  <div className="w-full aspect-square bg-gradient-to-br from-spotify-blue/20 to-spotify-green/20 rounded mb-3 flex items-center justify-center">
                    <ListMusic size={32} className="text-spotify-subtext" />
                  </div>
                )}
                <h3 className="font-medium text-sm truncate">{pl.name}</h3>
                <p className="text-xs text-spotify-subtext mt-1">
                  {pl.trackCount} 首 · {pl.isPublic ? '公开' : '私密'}
                </p>
              </motion.div>
            ))}
          </div>
        ) : (
          <div className="text-center py-20 text-spotify-subtext">
            <ListMusic size={48} className="mx-auto mb-3 opacity-50" />
            <p className="text-lg">还没有歌单</p>
            <p className="text-sm mt-1">从收藏的歌曲创建你的第一个歌单</p>
          </div>
        )}
      </div>
    </MainLayout>
  );
}
