'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { motion } from 'framer-motion';
import { useAuthStore } from '@/store/auth-store';

export default function LoginPage() {
  const router = useRouter();
  const { login, isLoading } = useAuthStore();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await login({ email, password });
      router.push('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : '登录失败，请重试');
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-spotify-black">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        className="w-full max-w-sm"
      >
        <div className="text-center mb-8">
          <h1 className="text-2xl font-bold">
            <span className="text-spotify-green">Music</span>Rec
          </h1>
          <p className="text-spotify-subtext mt-2">登录你的账号</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <div className="bg-spotify-red/10 border border-spotify-red/30 text-spotify-red text-sm rounded-md px-4 py-2">
              {error}
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-spotify-subtext mb-1">邮箱</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full px-4 py-2.5 bg-spotify-card rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-spotify-green"
              placeholder="name@example.com"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-spotify-subtext mb-1">密码</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-4 py-2.5 bg-spotify-card rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-spotify-green"
              placeholder="••••••••"
              required
            />
          </div>

          <button
            type="submit"
            disabled={isLoading}
            className="w-full py-2.5 bg-spotify-green text-black font-semibold rounded-full hover:bg-spotify-green-hover transition-colors disabled:opacity-50"
          >
            {isLoading ? '登录中...' : '登录'}
          </button>
        </form>

        <p className="text-center text-sm text-spotify-subtext mt-6">
          还没有账号？{' '}
          <Link href="/register" className="text-spotify-green hover:underline">
            立即注册
          </Link>
        </p>
      </motion.div>
    </div>
  );
}
