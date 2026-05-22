# 优化 001: 探索页面 — 种子数据生成 + 刷新机制

## 优化信息

| 项目 | 内容 |
|------|------|
| **优化时间** | 2026-05-22 13:00 |
| **优化类型** | 功能增强 — 新增种子数据生成 + 交互改进 |
| **影响模块** | 后端 Discovery 模块 + 前端探索页面 |

## 优化原因

1. **数据量不足**: 数据库仅 3 首手工导入曲目，探索引擎 70/20/10 分配和流派多样性采样无数据可用
2. **无刷新机制**: 前端探索页面使用 TanStack Query 但没有 refetch/刷新按钮，缓存 TTL 长（探索 10min，冷启动 1h）
3. **无种子入口**: 现有 `POST /api/recommendations/seed` 只为已有曲目生成音频特征，不创建曲目本身

## 优化策略

### 后端: 种子曲目生成器

新增 `POST /api/discovery/seed-tracks` 端点，自动生成覆盖 23 个流派的模拟数据：

- **23 流派**: electronic, pop, rock, jazz, classical, hip hop, r&b, metal, folk, country, blues, funk, soul, latin 等
- **34 艺术家**: 跨流派分布，名称唯一，含流派标签和流行度
- **26 专辑**: 每流派 1-2 张专辑，含发布年份和曲目数
- **188 首曲目**: 每张专辑 6-8 首，Popularity 20-95 均匀分布
- **幂等设计**: SpotifyId 使用 `seed_<type>_<name>` 确定性格式，重复调用零重复创建
- **模块隔离**: Controller 层编排 `SeedTracksCommand` + `SeedAudioFeaturesCommand`，不从 Discovery 引用 Recommendation

### 前端: 刷新 + 种子按钮

探索页面头部新增两个操作按钮：

1. **生成种子数据** (Database 图标): 
   - `useMutation` 调用 `POST /api/discovery/seed-tracks`
   - 完成后自动 refetch 推荐数据
   - 按钮禁用 + 旋转动画状态反馈

2. **刷新推荐** (RefreshCw 图标):
   - 直接调用 `refetch()` 绕过缓存
   - 图标旋转动画展示刷新状态
   - 未登录调用 `refetchColdStart()`，已登录调用 `refetchDiscovery()`

## 修改文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `src/Modules/Discovery/Commands/SeedTracksCommand.cs` | 新建 | 命令 + 结果 DTO 记录 |
| `src/Modules/Discovery/Handlers/SeedTracksCommandHandler.cs` | 新建 | ~350行，5 阶段种子数据生成 |
| `src/Web/MusicRec.WebApi/Controllers/DiscoveryController.cs` | 修改 | 新增 POST seed-tracks |
| `frontend/src/lib/api/discovery.ts` | 修改 | 新增 seedTracks() + SeedTracksResult |
| `frontend/src/app/explore/page.tsx` | 修改 | 新增操作按钮 + refetch/mutation |

## 优化前后对比

### 优化前
```
探索页面: 固定 3 首曲目, 无刷新按钮
数据库: 3 Tracks, 23 Genres (已有), 少量 Artist
用户操作: 无法获取更多曲目, 无法刷新推荐
```

### 优化后
```
探索页面: 20 首/页, 覆盖 20 种流派, 可刷新/可一键生成种子
数据库: 188+ Tracks, 34 Artists, 26 Albums, 23 Genres
用户操作: 点击"生成种子数据"→ 自动填充 → 刷新推荐
种子幂等: 重复调用 0 重复创建
```

## 验证结果

| 检查项 | 结果 |
|--------|------|
| `dotnet build` | 0 警告 0 错误 |
| `npx tsc --noEmit` | 0 错误 |
| `POST /api/discovery/seed-tracks` (首次) | 23 流派, 34 艺术家, 26 专辑, 188 曲目, 188 音频特征 |
| `POST /api/discovery/seed-tracks` (二次) | 全部 0 — 幂等 |
| `GET /api/discovery/cold-start?limit=20` | 20 首, 覆盖 20 种不同流派 (blues → techno) |
| 前端登录/注册页 | 200 OK |
