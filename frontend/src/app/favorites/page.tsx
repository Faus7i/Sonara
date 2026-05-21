'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { Heart } from 'lucide-react';
import { MainLayout } from '@/components/layout/MainLayout';
import { getFavorites, removeFavorite } from '@/lib/api/favorites';
import type { Track } from '@/types/api';

export default function FavoritesPage() {
  const queryClient = useQueryClient();

  const { data: favorites, isLoading } = useQuery({
    queryKey: ['favorites'],
    queryFn: () => getFavorites(1, 50),
  });

  const removeMutation = useMutation({
    mutationFn: removeFavorite,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['favorites'] });
    },
  });

  return (
    <MainLayout>
      <div className="p-6 max-w-screen-xl mx-auto">
        <motion.div initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} className="mb-8">
          <h2 className="text-2xl font-bold flex items-center gap-2">
            <Heart size={24} className="text-spotify-green" fill="#1ed760" />
            我的收藏
          </h2>
          <p className="text-spotify-subtext mt-1 text-sm">
            {favorites ? `${favorites.length} 首歌曲` : '加载中...'}
          </p>
        </motion.div>

        {isLoading ? (
          <div className="space-y-2">
            {Array.from({ length: 10 }).map((_, i) => (
              <div key={i} className="flex items-center gap-3 p-2">
                <div className="w-10 h-10 bg-spotify-hover rounded animate-pulse" />
                <div className="flex-1">
                  <div className="h-4 bg-spotify-hover rounded animate-pulse w-1/3 mb-1" />
                  <div className="h-3 bg-spotify-hover rounded animate-pulse w-1/4" />
                </div>
              </div>
            ))}
          </div>
        ) : favorites && favorites.length > 0 ? (
          <div className="space-y-1">
            {favorites.map((track: Track, i: number) => (
              <motion.div
                key={track.id}
                initial={{ opacity: 0, x: -10 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: i * 0.02 }}
                className="flex items-center gap-3 p-2 hover:bg-spotify-card rounded-md group transition-colors"
              >
                <span className="text-xs text-spotify-subtext w-6 text-right">{i + 1}</span>
                {track.coverImageUrl ? (
                  <img src={track.coverImageUrl} alt={track.name} className="w-10 h-10 rounded object-cover" loading="lazy" />
                ) : (
                  <div className="w-10 h-10 bg-spotify-hover rounded flex items-center justify-center text-xs">♪</div>
                )}
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">{track.name}</p>
                  <p className="text-xs text-spotify-subtext truncate">{track.artistsSummary}</p>
                </div>
                <button
                  onClick={() => removeMutation.mutate(track.id)}
                  className="text-spotify-subtext hover:text-spotify-red transition-colors opacity-0 group-hover:opacity-100"
                  aria-label="取消收藏"
                >
                  <Heart size={16} fill="#e91429" className="text-spotify-red" />
                </button>
              </motion.div>
            ))}
          </div>
        ) : (
          <div className="text-center py-20 text-spotify-subtext">
            <Heart size={48} className="mx-auto mb-3 opacity-50" />
            <p className="text-lg">还没有收藏歌曲</p>
            <p className="text-sm mt-1">探索和搜索你喜欢的音乐，点击收藏</p>
          </div>
        )}
      </div>
    </MainLayout>
  );
}
