'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { MainLayout } from '@/components/layout/MainLayout';
import { search } from '@/lib/api/search';
import type { SearchType, Track } from '@/types/api';

export default function SearchPage() {
  const [query, setQuery] = useState('');
  const [searchType, setSearchType] = useState<SearchType>('track');

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['search', query, searchType],
    queryFn: () => search(query, searchType, 20),
    enabled: query.length >= 2,
  });

  return (
    <MainLayout>
      <div className="p-6 max-w-screen-xl mx-auto">
        <motion.div initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} className="mb-8">
          <h2 className="text-2xl font-bold mb-4">搜索</h2>
          <div className="flex gap-2 items-center">
            <input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="搜索歌曲、艺术家、专辑..."
              className="flex-1 px-4 py-3 bg-spotify-card rounded-full text-sm text-spotify-text placeholder-spotify-subtext focus:outline-none focus:ring-2 focus:ring-spotify-green"
              autoFocus
            />
            <select
              value={searchType}
              onChange={(e) => setSearchType(e.target.value as SearchType)}
              className="px-3 py-3 bg-spotify-card rounded-full text-sm text-spotify-text focus:outline-none"
            >
              <option value="track">曲目</option>
              <option value="artist">艺术家</option>
              <option value="album">专辑</option>
            </select>
          </div>
        </motion.div>

        {query.length < 2 ? (
          <div className="text-center py-20 text-spotify-subtext">
            <p className="text-lg">输入关键词开始搜索</p>
          </div>
        ) : isFetching ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
            {Array.from({ length: 10 }).map((_, i) => (
              <div key={i} className="bg-spotify-card rounded-md p-3">
                <div className="w-full aspect-square bg-spotify-hover rounded animate-pulse mb-3" />
                <div className="h-4 bg-spotify-hover rounded animate-pulse mb-2" />
                <div className="h-3 bg-spotify-hover rounded animate-pulse w-2/3" />
              </div>
            ))}
          </div>
        ) : data ? (
          <div>
            {data.tracks && data.tracks.length > 0 && (
              <div className="mb-8">
                <h3 className="text-lg font-semibold mb-3">曲目</h3>
                <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
                  {data.tracks.map((track: Track, i: number) => (
                    <motion.div
                      key={track.id}
                      initial={{ opacity: 0, y: 10 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ delay: i * 0.03 }}
                      className="bg-spotify-card hover:bg-spotify-hover rounded-md p-3 cursor-pointer transition-colors"
                    >
                      <div className="mb-3">
                        {track.coverImageUrl ? (
                          <img src={track.coverImageUrl} alt={track.name} className="w-full aspect-square object-cover rounded shadow-lg" loading="lazy" />
                        ) : (
                          <div className="w-full aspect-square bg-spotify-hover rounded flex items-center justify-center text-3xl">♪</div>
                        )}
                      </div>
                      <h4 className="font-medium text-sm truncate">{track.name}</h4>
                      <p className="text-xs text-spotify-subtext truncate mt-1">{track.artistsSummary}</p>
                    </motion.div>
                  ))}
                </div>
              </div>
            )}
            {!data.tracks?.length && (
              <div className="text-center py-10 text-spotify-subtext">
                <p>没有找到结果</p>
              </div>
            )}
          </div>
        ) : null}
      </div>
    </MainLayout>
  );
}
