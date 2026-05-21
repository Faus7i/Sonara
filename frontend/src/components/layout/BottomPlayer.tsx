'use client';

/**
 * 底部播放器 — 当前为占位组件
 * 需要 Spotify Premium 和 Web Playback SDK 才能完整使用
 */
export function BottomPlayer() {
  return (
    <div className="h-18 fixed bottom-0 left-0 right-0 z-50 glass border-t border-spotify-card px-4">
      <div className="flex items-center justify-between h-full max-w-screen-xl mx-auto">
        {/* 左侧：当前播放信息 */}
        <div className="flex items-center gap-3 w-72">
          <div className="w-12 h-12 bg-spotify-card rounded flex items-center justify-center text-spotify-subtext text-xs">
            ♪
          </div>
          <div>
            <p className="text-sm font-medium text-spotify-subtext">未在播放</p>
            <p className="text-xs text-spotify-subtext/60">通过 Spotify 播放</p>
          </div>
        </div>

        {/* 中间：播放控制 */}
        <div className="flex items-center gap-4">
          <button className="text-spotify-subtext hover:text-spotify-text transition-colors" aria-label="随机播放">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <path d="M13.151.922a.75.75 0 10-1.06 1.06L13.109 3H11.16a3.75 3.75 0 00-2.873 1.34l-6.173 7.356A2.25 2.25 0 01.39 12.5H0V14h.391a3.75 3.75 0 002.873-1.34l6.173-7.356A2.25 2.25 0 0111.16 4.5h1.95l-1.02 1.02a.75.75 0 101.06 1.06L15.98 3.85a.75.75 0 000-1.06l-2.829-2.87z"/>
            </svg>
          </button>
          <button className="text-spotify-subtext hover:text-spotify-text transition-colors" aria-label="上一首">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <path d="M3.3 1a.7.7 0 01.7.7v5.15l9.95-5.744a.7.7 0 011.05.606v12.575a.7.7 0 01-1.05.606L4 9.149V14.3a.7.7 0 01-.7.7H1.7a.7.7 0 01-.7-.7V1.7a.7.7 0 01.7-.7h1.6z"/>
            </svg>
          </button>
          <button className="w-8 h-8 bg-spotify-text rounded-full flex items-center justify-center hover:scale-105 transition-transform" aria-label="播放">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="#121212">
              <path d="M3 1.713a.7.7 0 011.05-.607l10.89 6.288a.7.7 0 010 1.212L4.05 14.894A.7.7 0 013 14.288V1.713z"/>
            </svg>
          </button>
          <button className="text-spotify-subtext hover:text-spotify-text transition-colors" aria-label="下一首">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <path d="M12.7 1a.7.7 0 00-.7.7v5.15L2.05 1.107A.7.7 0 001 1.712v12.575a.7.7 0 001.05.607L12 9.149V14.3a.7.7 0 00.7.7h1.6a.7.7 0 00.7-.7V1.7a.7.7 0 00-.7-.7h-1.6z"/>
            </svg>
          </button>
          <button className="text-spotify-subtext hover:text-spotify-text transition-colors" aria-label="循环播放">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <path d="M0 4.75A3.75 3.75 0 013.75 1h8.5A3.75 3.75 0 0116 4.75v5a3.75 3.75 0 01-3.75 3.75H9.81l1.018 1.018a.75.75 0 11-1.06 1.06L6.939 12.75l2.829-2.828a.75.75 0 111.06 1.06L9.811 12h2.439a2.25 2.25 0 002.25-2.25v-5a2.25 2.25 0 00-2.25-2.25h-8.5A2.25 2.25 0 001.5 4.75v5A2.25 2.25 0 003.75 12H5v1.5H3.75A3.75 3.75 0 010 9.75v-5z"/>
            </svg>
          </button>
        </div>

        {/* 右侧：音量 */}
        <div className="flex items-center gap-2 w-72 justify-end">
          <button className="text-spotify-subtext hover:text-spotify-text transition-colors" aria-label="音量">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <path d="M9.741.85a.75.75 0 01.375.65v13a.75.75 0 01-1.125.65l-6.925-4a3.642 3.642 0 01-1.33-4.967 3.639 3.639 0 011.33-1.332l6.925-4a.75.75 0 01.75 0zm-6.924 5.3a2.139 2.139 0 000 3.7l5.8 3.35V2.8l-5.8 3.35zm8.683 4.29V5.56a2.75 2.75 0 010 4.88z"/>
            </svg>
          </button>
          <div className="w-24 h-1 bg-spotify-hover rounded-full">
            <div className="w-3/4 h-full bg-spotify-text rounded-full" />
          </div>
        </div>
      </div>
    </div>
  );
}
