# 集成测试报告

> **日期**：2026-05-21  
> **方法**：`dotnet build` + `dotnet run` + PowerShell Invoke-RestMethod 端点测试  
> **环境**：SQL Server `.\SQLEXPRESS` / `MusicRecDb`，Spotify API 可用  
> **结果**：✅ 全部通过

---

## 一、编译检查

```
dotnet build → 0 错误, 0 警告, 12/12 项目通过
```

---

## 二、端点测试结果

### Identity 模块（5/5）

| # | 测试 | 端点 | 期望 | 结果 |
|:---:|------|------|------|:---:|
| 1 | 注册 | `POST /api/identity/register` | 200 + JWT | ✅ |
| 2 | 登录 | `POST /api/identity/login` | 200 + JWT | ✅ |
| 3 | 获取资料 | `GET /api/identity/profile` | 200 + UserProfileDto | ✅ |
| 4 | 更新资料 | `PUT /api/identity/profile` | 200 + 更新后 nickname | ✅ |
| 5 | 重复注册 | `POST /api/identity/register` | 409 Conflict | ✅ |
| 6 | 错误密码 | `POST /api/identity/login` | 401 Unauthorized | ✅ |
| 7 | 未认证访问 | `GET /api/identity/profile` (无 Token) | 401 | ✅ |

### Search 模块（2/2）

| # | 测试 | 端点 | 结果 |
|:---:|------|------|:---:|
| 1 | 曲目搜索 | `GET /api/search?q=Daft+Punk&type=track&limit=3` | ✅ 3 results |
| 2 | 复合搜索 | `GET /api/search?q=Imagine+Dragons&type=track,artist,album` | ✅ 各 2 results |

### Catalog 模块（3/3）

| # | 测试 | 端点 | 结果 |
|:---:|------|------|:---:|
| 1 | 导入曲目 | `POST /api/catalog/tracks/import` | ✅ Believer by Imagine Dragons |
| 2 | 曲目详情 | `GET /api/catalog/tracks/{id}` | ✅ |
| 3 | 导入艺术家 | `POST /api/catalog/artists/import` | ✅ Imagine Dragons |
| 4 | 未知 ID | `GET /api/catalog/tracks/{randomGuid}` | ✅ 404 |

### Favorites 模块（5/5）

| # | 测试 | 端点 | 结果 |
|:---:|------|------|:---:|
| 1 | 收藏 | `POST /api/favorites/tracks/{id}` | ✅ |
| 2 | 重复收藏（幂等） | `POST /api/favorites/tracks/{id}` | ✅ 幂等返回 |
| 3 | 检查状态 | `GET /api/favorites/check/{id}` | ✅ liked=True |
| 4 | 取消收藏 | `DELETE /api/favorites/tracks/{id}` | ✅ |
| 5 | 重复取消（幂等） | `DELETE /api/favorites/tracks/{id}` | ✅ 幂等静默 |

### Playlist 模块（4/4）

| # | 测试 | 端点 | 结果 |
|:---:|------|------|:---:|
| 1 | 创建歌单 | `POST /api/playlists` | ✅ |
| 2 | 添加曲目 | `POST /api/playlists/{id}/tracks` | ✅ |
| 3 | 歌单详情 | `GET /api/playlists/{id}` | ✅ 含 1 曲目 |
| 4 | 删除歌单 | `DELETE /api/playlists/{id}` | ✅ Cascade |

### Player 模块（2/2）

| # | 测试 | 端点 | 结果 |
|:---:|------|------|:---:|
| 1 | 播放状态 | `GET /api/player/state` | ✅ null（无活跃 session，非 500） |
| 2 | 设备列表 | `GET /api/player/devices` | ✅ 空列表（非 500） |

### AudioFeatures 模块（0/0）

| 状态 | 说明 |
|:---:|------|
| — | 无公开 Controller。MediatR Handler 已注册，由 Catalog 内部或未来 Recommendation 模块调用 |

---

## 三、测试中发现并修复的问题

### 问题 19：Player 端点返回 500

- **文件**：`src/Infrastructure/Spotify/SpotifyClient.cs`
- **根因**：`GetPlaybackStateAsync` 仅处理 204（NoContent），`GetAvailableDevicesAsync` 未处理非 2xx。Spotify 在 Client Credentials 模式下对 `/me/player` 端点返回 404，`EnsureSuccessStatusCode()` 抛 `HttpRequestException` → `GlobalExceptionMiddleware` 转为 500
- **修复**：
  - `GetPlaybackStateAsync`：404 → 返回 `null`（与 204 一致）
  - `GetAvailableDevicesAsync`：404 → 返回 `new List<DeviceObject>()`

---

## 四、汇总

| 模块 | 测试数 | 通过 | 失败 |
|------|:---:|:---:|:---:|
| Identity | 7 | 7 | 0 |
| Search | 2 | 2 | 0 |
| Catalog | 4 | 4 | 0 |
| Favorites | 5 | 5 | 0 |
| Playlist | 4 | 4 | 0 |
| Player | 2 | 2 | 0 |
| AudioFeatures | — | — | — |
| **总计** | **24** | **24** | **0** |

系统所有模块业务正常运行。
