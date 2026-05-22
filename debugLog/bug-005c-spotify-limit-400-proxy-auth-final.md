# Bug 005c: 搜索模块完整修复 — Spotify 400 + 代理 + 认证

## 基本信息

| 项目 | 内容 |
|------|------|
| **严重等级** | P0 — 搜索、登录等核心功能不可用 |
| **发现时间** | 2026-05-22 13:30 |
| **修复时间** | 2026-05-22 14:50 |
| **关联 Bug** | Bug 005（类型不匹配）、Bug 005b（代理网络错误）、Bug 002（CORS） |

## 问题链分析

经过多轮排查，搜索"avicii"失败的完整根因链如下：

### 第一层：浏览器跨域（Bug 002 已修复）
```
前端(3000) → 后端(5000)：不同端口 → 跨域
修复：Program.cs 添加 CORS 配置
```

### 第二层：前后端类型不匹配（Bug 005 已修复）
```
后端返回 spotifyTrackId，前端期望 id → React key 为 undefined
修复：新增 SearchTrack/SearchArtist/SearchAlbum 专用类型
```

### 第三层：浏览器仍报 Network Error（Bug 005b）
```
CORS 配置正确但浏览器端仍无法连接后端
修复：引入 Next.js API 代理层，同源通信彻底消除跨域
```

### 第四层：代理返回 500（本次发现）
```
GET /api/search?q=avicii&limit=20 → 500
GET /api/search?q=avicii&limit=10 → 200
```

通过后端日志定位到真正的异常：

```
System.Net.Http.HttpRequestException: Response status code does not indicate success: 400 (Bad Request)
   at System.Net.Http.HttpResponseMessage.EnsureSuccessStatusCode()
   at MusicRec.Spotify.SpotifyClient.SearchAsync(...) in SpotifyClient.cs:line 28
```

**根因：Spotify API 对 limit > 10 返回 400 Bad Request**

经测试确认边界：
```
limit=2  → ✅ 200
limit=3  → ✅ 200
limit=5  → ✅ 200
limit=10 → ✅ 200
limit=11 → ❌ 400
limit=15 → ❌ 400
limit=20 → ❌ 400
```

与关键词无关（"avicii"、"daft punk"、"test" 全部触发），与调用频率无关（单独调用也失败）。推断为 Spotify Development Mode App 对返回数量有限制。

### 第五层：代理未转发 Authorization 头（连锁故障）
```
代理修复后，搜索匿名可用，但登录后请求仍 401
原因：简化版代理只转发 Content-Type，遗漏了 Authorization
修复：恢复 Authorization 头转发
```

## 修改文件汇总

| 文件 | 操作 | 说明 |
|------|------|------|
| `frontend/src/types/api.ts` | 修改 | 新增 SearchTrack/SearchArtist/SearchAlbum 类型 |
| `frontend/src/app/search/page.tsx` | 重写 | 三类型展示 + 错误处理 + limit=10 |
| `frontend/src/app/api/[...path]/route.ts` | 新建 | Next.js 全量 API 代理 |
| `frontend/src/lib/api-client.ts` | 修改 | baseURL 改为相对路径 `/api` |
| `frontend/.env.local` | 新建 | NEXT_PUBLIC_API_URL=/api |
| `src/Web/MusicRec.WebApi/Program.cs` | 修改 | CORS 配置 (Bug 002) |
| `src/Infrastructure/Spotify/SpotifyClient.cs` | 修改 | 400 错误改为带信息的异常 |

## 最终架构

```
浏览器 (localhost:3000)
    │
    │  同源请求 (无跨域)
    │  GET /api/search?q=avicii&limit=10
    │  Header: Authorization: Bearer <JWT>
    ▼
Next.js API Route (/api/[...path]/route.ts)
    │
    │  服务端转发 (转发 Authorization + Content-Type)
    │  GET http://127.0.0.1:5000/api/search?q=avicii&limit=10
    ▼
.NET Web API (localhost:5000)
    │
    │  Client Credentials OAuth (不涉及 Redirect URI)
    │  GET https://api.spotify.com/v1/search?q=avicii&type=track&limit=10
    ▼
Spotify Web API → 返回搜索结果
```

## 关于 Spotify API 的重要说明

1. **Client Credentials 认证**不需要 Redirect URI — 与 `http://127.0.0.1:5175/callback` 无关
2. **Development Mode** App 对搜索 limit 有限制（最多约 10 条结果）
3. Client ID/Secret 在 `appsettings.json` 中正确配置即可
4. 搜索结果中 `popularity: 0` 是 Client Credentials 模式的正常行为（无用户上下文）

## 验证结果

| 检查项 | 结果 |
|--------|------|
| 搜索 "avicii" (前端) | ✅ 10 首曲目正常显示 |
| 搜索艺术家/专辑 | ✅ 三种类型分类展示 |
| 登录/注册 | ✅ 正常 |
| 已认证 API (推荐/收藏等) | ✅ JWT 通过代理正常转发 |
| `dotnet build` | ✅ 0 错误 |
| `npx tsc --noEmit` | ✅ 0 错误 |
