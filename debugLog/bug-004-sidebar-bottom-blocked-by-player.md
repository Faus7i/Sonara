# Bug 004: 底部播放栏遮挡侧边栏退出登录按钮

## 基本信息

| 项目 | 内容 |
|------|------|
| **严重等级** | P2 — UI 遮挡，交互元素不可达 |
| **发现时间** | 2026-05-22 12:35 |
| **修复时间** | 2026-05-22 12:36 |
| **影响模块** | 前端 — Sidebar 布局 |
| **影响范围** | 已登录状态下侧边栏底部的"退出登录"按钮被播放栏遮挡 |

## 问题描述

用户登录后，侧边栏底部的用户信息区域和"退出登录"按钮被底部播放栏 (`BottomPlayer`) 遮挡，用户无法点击退出。

## 具体表现

1. 底部播放栏 (`fixed bottom-0, z-50, h-18`) 始终浮动在页面最底部
2. 侧边栏高度撑满 (`h-full`)，认证区域在底部，恰好被播放栏覆盖
3. 主内容区使用了 `pb-24` 为播放栏留出空间，但侧边栏没有对应的底部内边距

## 触发条件

- 用户登录后查看侧边栏底部
- 任何带有侧边栏的页面（首页、探索、搜索等）

## 根因分析

`MainLayout` 的布局结构：
```html
<div class="flex h-screen flex-col">        <!-- 全屏高度 -->
  <div class="flex flex-1 overflow-hidden">   <!-- 填充剩余空间 -->
    <Sidebar />                                <!-- h-full = 100% 父容器高度 -->
    <main class="pb-24" />                     <!-- 有底部留白，避免被播放栏遮挡 -->
  </div>
  <BottomPlayer />                           <!-- fixed bottom-0, z-50 浮动定位 -->
</div>
```

- `BottomPlayer` 虽然是 `MainLayout` 的子元素，但使用了 `fixed` 定位，脱离文档流
- `Sidebar` 使用 `h-full`，高度等于父 flex 容器的全部高度（等于视口高度 - 没有为播放栏预留空间）
- `main` 内容区通过 `pb-24` (96px) 预留底部空间，但 `Sidebar` 缺少对应设置

## 修复策略

在 `Sidebar` 的最外层容器添加 `pb-20`（5rem = 80px），为底部播放栏留出空间：

```diff
- <aside className="... h-full shrink-0 border-r border-spotify-card">
+ <aside className="... h-full shrink-0 border-r border-spotify-card pb-20">
```

`pb-20` (80px) 足以容纳 `h-18` (72px) 的播放栏高度。与主内容区的 `pb-24` (96px) 相比略小，因为侧边栏的认证区域从 `pt-3` 开始有额外间距。

## 修改文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `frontend/src/components/layout/Sidebar.tsx` | 修改 | `<aside>` 添加 `pb-20` |

## 修复前后对比

### 修复前
```html
<aside className="... h-full shrink-0 border-r border-spotify-card">
  <!-- 导航 + 认证区域，底部被播放栏遮挡 -->
</aside>
```

### 修复后
```html
<aside className="... h-full shrink-0 border-r border-spotify-card pb-20">
  <!-- 导航 + 认证区域，底部有 80px 留白，不被遮挡 -->
</aside>
```

## 验证结果

| 检查项 | 结果 |
|--------|------|
| TypeScript 类型检查 | 0 错误 |
