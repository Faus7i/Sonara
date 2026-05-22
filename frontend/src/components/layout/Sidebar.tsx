'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { Home, Compass, Search, Heart, ListMusic, LogIn, UserPlus, User, LogOut } from 'lucide-react';
import { useAuthStore } from '@/store/auth-store';

const navItems = [
  { href: '/', label: '首页', icon: Home },
  { href: '/explore', label: '探索', icon: Compass },
  { href: '/search', label: '搜索', icon: Search },
  { href: '/favorites', label: '收藏', icon: Heart, requiresAuth: true },
  { href: '/playlists', label: '歌单', icon: ListMusic, requiresAuth: true },
];

export function Sidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const { isAuthenticated, user, logout } = useAuthStore();

  const handleLogout = () => {
    logout();
    router.push('/');
  };

  return (
    <aside className="hidden md:flex w-60 flex-col bg-spotify-black h-full shrink-0 border-r border-spotify-card pb-20">
      {/* Logo */}
      <div className="p-5">
        <h1 className="text-xl font-bold tracking-tight">
          <span className="text-spotify-green">Music</span>Rec
        </h1>
      </div>

      {/* 导航 */}
      <nav className="flex-1 px-3">
        <ul className="space-y-1">
          {navItems.map(({ href, label, icon: Icon, requiresAuth }) => {
            const isActive = pathname === href;
            if (requiresAuth && !isAuthenticated) return null;
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

      {/* 用户认证区域 */}
      <div className="px-3 pb-4 border-t border-spotify-card pt-3">
        {isAuthenticated ? (
          <div className="space-y-1">
            <div className="flex items-center gap-3 px-3 py-2.5 text-sm">
              <div className="w-8 h-8 rounded-full bg-spotify-green flex items-center justify-center text-black font-semibold text-xs shrink-0">
                {user?.nickname?.charAt(0)?.toUpperCase() || user?.email?.charAt(0)?.toUpperCase() || 'U'}
              </div>
              <div className="flex-1 min-w-0">
                <p className="font-medium text-sm truncate">{user?.nickname || '用户'}</p>
                {user?.email ? (
                  <p className="text-xs text-spotify-subtext truncate">{user.email}</p>
                ) : null}
              </div>
            </div>
            <button
              onClick={handleLogout}
              className="flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium text-spotify-subtext hover:text-spotify-text hover:bg-spotify-card transition-colors w-full"
            >
              <LogOut size={18} />
              退出登录
            </button>
          </div>
        ) : (
          <div className="space-y-1">
            <Link
              href="/login"
              className="flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium text-spotify-subtext hover:text-spotify-text hover:bg-spotify-card transition-colors"
            >
              <LogIn size={20} />
              登录
            </Link>
            <Link
              href="/register"
              className="flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium text-spotify-subtext hover:text-spotify-text hover:bg-spotify-card transition-colors"
            >
              <UserPlus size={20} />
              注册
            </Link>
          </div>
        )}
      </div>
    </aside>
  );
}
