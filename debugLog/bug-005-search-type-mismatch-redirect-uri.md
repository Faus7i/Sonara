# Bug 005: 搜索模块无法正常搜索 — Redirect URI 误判 + 前后端类型不匹配

## 基本信息

| 项目 | 内容 |
|------|------|
| **严重等级** | P1 — 核心功能部分不可用（搜索可用但前端渲染异常） |
| **发现时间** | 2026-05-22 13:30 |
| **修复时间** | 2026-05-22 13:45 |
| **影响模块** | 前端搜索页面 + 类型定义 |
| **影响范围** | 搜索结果显示（曲目 key 异常 + 艺术家/专辑完全不展示） |

## 问题分析

### 问题 1: Redirect URI 疑虑（非 Bug）

用户误认为需要配置 Spotify Redirect URI 才能搜索。

**解答**：当前项目使用 **Client Credentials OAuth**（服务器到服务器认证），**不需要任何 Redirect URI**。Redirect URI 仅用于 Authorization Code 流程（用户通过浏览器授权登录 Spotify 账号）。项目的 `appsettings.json` 中已正确配置 ClientId 和 ClientSecret，搜索 API 正常响应。

### 问题 2: 前后端类型不匹配（核心 Bug）

后端 `SearchTrackDto` 返回字段：`spotifyTrackId`, `name`, `durationMs`, `popularity`, `coverImageUrl`, `albumName`, `artistsSummary`

前端旧 `Track` 类型期望：`id`, `name`, `coverImageUrl`, `durationMs`, `popularity`, `artistsSummary`, `albumName`

**`id` vs `spotifyTrackId` 不匹配** — 后端不返回 `id` 字段，前端用 `track.id` 作为 React key 得到 `undefined`。

类似问题存在于艺术家（`spotifyArtistId` vs `id`）和专辑（`spotifyAlbumId` vs `id`）。

### 问题 3: 搜索页面只显示曲目（功能缺失）

原搜索页面只渲染 `data.tracks`，即使选择"艺术家"或"专辑"类型搜索，结果也不显示。

## 具体表现

1. 搜索曲目时 `track.id` = `undefined`（React key 警告）
2. 搜索艺术家/专辑时结果显示为空（类型不匹配导致条件判断失败）
3. 即使数据正确返回，艺术家和专辑结果也不渲染
4. `popularity` 字段虽后端返回但前端旧类型中未使用

## 触发条件

- 用户在搜索页面执行任何搜索操作
- 选择非"曲目"类型搜索（艺术家/专辑完全不可见）

## 根因分析

三个问题的叠加：

1. **类型定义过于宽泛**：`SearchResult` 直接复用 `Track`/`ArtistBrief`/`AlbumBrief` 类型，但这些类型是为已导入曲目设计的（有 `id` 字段），搜索结果来自 Spotify API 实时返回（只有 `spotifyXxxId`）
2. **搜索页面功能残缺**：只实现了曲目卡片展示，艺术家和专辑完全没有对应的 UI 组件
3. **后端返回全部类型**：`SearchResultDto` 包含 `tracks`、`artists`、`albums` 三个数组，但前端只处理了 `tracks`

## 修复策略

### 1. 新增搜索专用类型 (`types/api.ts`)

创建与后端 `SearchTrackDto`/`SearchArtistDto`/`SearchAlbumDto` 精确匹配的前端类型：

```typescript
export interface SearchTrack {
  spotifyTrackId: string;  // ← 精确匹配后端字段名
  name: string;
  durationMs: number;
  popularity: number;
  coverImageUrl: string | null;
  albumName: string;
  artistsSummary: string;
}

export interface SearchArtist {
  spotifyArtistId: string;
  name: string;
  genres: string | null;
  imageUrl: string | null;
  popularity: number;
}

export interface SearchAlbum {
  spotifyAlbumId: string;
  name: string;
  releaseDate: string;
  coverImageUrl: string | null;
  albumType: string;
  artistsSummary: string;
}

export interface SearchResult {
  tracks: SearchTrack[];
  artists: SearchArtist[];
  albums: SearchAlbum[];
}
```

### 2. 重构搜索页面 (`app/search/page.tsx`)

- **TrackCard**: 使用 `spotifyTrackId` 作为 React key，增加 `albumName` 展示
- **ArtistCard**（新增）: 圆形头像 + 流派标签（逗号转分隔符）+ 热度值
- **AlbumCard**（新增）: 封面 + 艺术家 + 发行日期/类型
- 三种结果区域独立渲染，根据数据类型分别展示
- 无结果时统一提示

## 修改文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `frontend/src/types/api.ts` | 修改 | 新增 SearchTrack/SearchArtist/SearchAlbum 类型 |
| `frontend/src/app/search/page.tsx` | 重写 | 三种类型完整展示 + 字段名修正 |
| `frontend/src/lib/api/search.ts` | 修改 | 新增 searchTracks/searchArtists/searchAlbums |

## 修复前后对比

### 修复前
```
搜索 "daft punk" → API 返回 5 首曲目 + 3 位艺术家 + 3 张专辑
前端展示: 仅 5 首曲目（key=undefined, React 警告）, 艺术家/专辑不可见
切换类型为"艺术家" → 无结果显示
```

### 修复后
```
搜索 "daft punk" → API 返回相同数据
前端展示: 曲目区段 5 首 + 艺术家区段 3 位（圆形头像+流派） + 专辑区段 3 张（封面+日期）
切换类型为"艺术家" → 显示艺术家搜索结果
切换类型为"专辑" → 显示专辑搜索结果
```

## 验证结果

| 检查项 | 结果 |
|--------|------|
| TypeScript 类型检查 | 0 错误 |
| `curl /api/search?q=daft+punk&type=track` | 5 首曲目返回 |
| `curl /api/search?q=daft+punk&type=artist` | 3 位艺术家返回 |
| `curl /api/search?q=discovery&type=album` | 3 张专辑返回 |
| Redirect URI | 无需修改 — Client Credentials 流程不需要 |

## 关于 Spotify API 配置说明

用户提供的 Spotify App 配置完全正确：
- **Client ID**: `352220271e18487297cdc08735535e34` ✅
- **Client Secret**: `73ddfe38b99c4efbbfc68f35a90eb5b0` ✅（已配置在 appsettings.json）
- **Redirect URI**: `http://127.0.0.1:5175/callback` — **不需要修改**，当前项目不使用
- **API**: Web API + Web Playback SDK ✅

Spotify 使用 Client Credentials 认证流程获取 Access Token 进行搜索，这是服务器到服务器的通信，不涉及用户授权和浏览器重定向。
