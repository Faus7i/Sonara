'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Home, Compass, Search, Heart, ListMusic } from 'lucide-react';

const navItems = [
  { href: '/', label: '首页', icon: Home },
  { href: '/explore', label: '探索', icon: Compass },
  { href: '/search', label: '搜索', icon: Search },
  { href: '/favorites', label: '收藏', icon: Heart },
  { href: '/playlists', label: '歌单', icon: ListMusic },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="hidden md:flex w-60 flex-col bg-spotify-black h-full shrink-0 border-r border-spotify-card">
      {/* Logo */}
      <div className="p-5">
        <h1 className="text-xl font-bold tracking-tight">
          <span className="text-spotify-green">Music</span>Rec
        </h1>
      </div>

      {/* 导航 */}
      <nav className="flex-1 px-3">
        <ul className="space-y-1">
          {navItems.map(({ href, label, icon: Icon }) => {
            const isActive = pathname === href;
            return (
              <li key={href}>
                <Link
                  href={href}
                  className={`flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium transition-colors ${
                    isActive
                      ? 'bg-spotify-card text-spotify-text'
                      : 'text-spotify-subtext hover:text-spotify-text hover:bg-spotify-card'
                  }`}
                >
                  <Icon size={20} />
                  {label}
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>
    </aside>
  );
}
