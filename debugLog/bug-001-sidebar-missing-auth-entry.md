# Bug 001: 侧边栏缺少登录/注册入口，用户无法找到登录模块

## 基本信息

| 项目 | 内容 |
|------|------|
| **严重等级** | P1 — 核心用户体验缺陷 |
| **发现时间** | 2026-05-22 11:44 |
| **修复时间** | 2026-05-22 11:50 |
| **影响模块** | 前端 — 全局侧边栏导航 |
| **影响范围** | 所有页面的侧边栏（首页、探索、搜索、收藏、歌单） |

## 问题描述

用户打开项目前端 (http://localhost:3000) 后，侧边栏仅显示 5 个导航链接：
- 首页、探索、搜索、收藏、歌单

**完全没有登录、注册、用户信息、退出登录等任何认证相关的 UI 元素。**
用户无法通过正常交互找到登录入口，只能手动在浏览器地址栏输入 `/login` 才能访问登录页面。

## 具体表现

1. **未登录状态**: 侧边栏底部无"登录"/"注册"按钮，用户不知道如何登录
2. **已登录状态**: 侧边栏无用户信息展示、无"退出登录"按钮
3. **收藏/歌单入口**: 未登录时仍显示"收藏"和"歌单"链接，点击后因无 Token 而报错

## 触发条件

- 任何用户打开网站首页或任何带有侧边栏的页面
- 浏览器宽度 >= 768px（md 断点以上显示侧边栏）

## 根因分析

`Sidebar.tsx` 组件是一个纯静态导航组件，完全没有导入或使用 `useAuthStore`，不感知用户认证状态：

```tsx
// 修复前 — 无任何 auth 引入
import { Home, Compass, Search, Heart, ListMusic } from 'lucide-react';
```

侧边栏设计上缺乏"认证区域"的概念，导航项对所有用户无差别展示。

## 修复策略

在侧边栏底部新增**用户认证区域**，根据 `useAuthStore` 的 `isAuthenticated` 状态切换显示：

### 未登录状态
- "登录"按钮（LogIn 图标）→ 导航到 `/login`
- "注册"按钮（UserPlus 图标）→ 导航到 `/register`

### 已登录状态
- 用户头像圆圈（首字母） + 昵称 + 邮箱
- "退出登录"按钮（LogOut 图标）→ 清除状态并跳转首页

### 附加改进
- "收藏"和"歌单"导航项标记 `requiresAuth: true`，未登录时自动隐藏，避免用户点击后报错

## 修改文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `frontend/src/components/layout/Sidebar.tsx` | 修改 | 新增认证区域 + 新增图标导入 + 引入 useAuthStore |

## 修复前后对比

### 修复前
```tsx
// 无 auth 引入
import { Home, Compass, Search, Heart, ListMusic } from 'lucide-react';

// 导航项无 requiresAuth，全部直接显示
const navItems = [
  { href: '/', label: '首页', icon: Home },
  { href: '/explore', label: '探索', icon: Compass },
  { href: '/search', label: '搜索', icon: Search },
  { href: '/favorites', label: '收藏', icon: Heart },
  { href: '/playlists', label: '歌单', icon: ListMusic },
];

// 侧边栏底部无认证区域
// </nav> 后直接 </aside>
```

### 修复后
```tsx
// 新增 auth 相关图标和 store 引入
import { useAuthStore } from '@/store/auth-store';
import { LogIn, UserPlus, LogOut } from 'lucide-react';

// 收藏/歌单标记 requiresAuth
const navItems = [
  ...
  { href: '/favorites', label: '收藏', icon: Heart, requiresAuth: true },
  { href: '/playlists', label: '歌单', icon: ListMusic, requiresAuth: true },
];

// 渲染时过滤未登录不可见项
if (requiresAuth && !isAuthenticated) return null;

// 侧边栏底部 — 新增认证区域
<div className="px-3 pb-4 border-t border-spotify-card pt-3">
  {isAuthenticated ? (
    /* 用户信息 + 退出登录 */
    <div className="space-y-1">
      <div className="flex items-center gap-3 px-3 py-2.5 text-sm">
        <div className="w-8 h-8 rounded-full bg-spotify-green ...">
          {user?.nickname?.charAt(0)?.toUpperCase() || 'U'}
        </div>
        <div className="flex-1 min-w-0">
          <p className="font-medium text-sm truncate">{user?.nickname || '用户'}</p>
          {user?.email ? <p className="text-xs text-spotify-subtext truncate">{user.email}</p> : null}
        </div>
      </div>
      <button onClick={handleLogout} ...>
        <LogOut size={18} /> 退出登录
      </button>
    </div>
  ) : (
    /* 登录 + 注册 */
    <div className="space-y-1">
      <Link href="/login" ...><LogIn size={20} /> 登录</Link>
      <Link href="/register" ...><UserPlus size={20} /> 注册</Link>
    </div>
  )}
</div>
```

## 验证方式

1. 打开 http://localhost:3000 → 侧边栏底部应显示"登录"和"注册"按钮
2. 点击"登录" → 跳转到 `/login` 登录页面
3. 点击"注册" → 跳转到 `/register` 注册页面
4. 登录后 → 侧边栏底部显示用户头像、昵称、邮箱和"退出登录"按钮
5. 未登录时 → "收藏"和"歌单"链接隐藏
6. TypeScript 类型检查: `npx tsc --noEmit` — 0 错误
