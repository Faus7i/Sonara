'use client';

import { Sidebar } from './Sidebar';
import { BottomPlayer } from './BottomPlayer';

export function MainLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex h-screen flex-col bg-spotify-black">
      <div className="flex flex-1 overflow-hidden">
        <Sidebar />
        <main className="flex-1 overflow-y-auto pb-24">
          {children}
        </main>
      </div>
      <BottomPlayer />
    </div>
  );
}
