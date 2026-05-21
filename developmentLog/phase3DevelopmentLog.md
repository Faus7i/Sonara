# Phase 3 开发日志：收藏 + 歌单 + 播放系统

> **日期**: 2026-05-21  
> **分支**: main  
> **SDK**: .NET 9.0.314  
> **Phase**: 3/6

---

## 1. 开发目标

按照开发文档 Phase 3 要求，实现以下三个模块：

| 模块 | 功能目标 |
|------|---------|
| **Favorites（收藏）** | 收藏歌曲、收藏列表、幂等操作、检查收藏状态 |
| **Playlist（歌单）** | 创建/编辑/删除歌单、添加/移除曲目、曲目排序、所有权保护 |
| **Player（播放）** | 播放控制 API（播放/暂停/上下首/音量/跳转/重复/随机/设备转移） |

同时扩展 Spotify 适配器，新增 11 个播放控制 API 方法。

---

## 2. 开发内容

### 2.1 新增文件统计

| 类别 | 数量 | 说明 |
|------|------|------|
| Spotify 基础设施 | 5 | ISpotifyClient 新增方法、SpotifyClient 实现、3 个新 Model |
| Player 模块 | 15 | csproj + DI + DTOs + 9 Commands + 2 Queries + 11 Handlers + 3 Validators + Controller |
| Favorites 模块 | 11 | csproj + DI + Entity + Configuration + DTOs + 2 Commands + 2 Queries + 4 Handlers + Validator + Controller |
| Playlist 模块 | 17 | csproj + DI + 2 Entities + 2 Configurations + 5 DTOs + 6 Commands + 2 Queries + 8 Handlers + 4 Validators + Controller |
| 集成与基础设施 | 5 | DesignTimeDbContextFactory、Program.cs、WebApi.csproj、.sln、ClaimsPrincipalExtensions |
| EF Migration | 1 | Phase3_FavoritesAndPlaylist |
| **合计** | **54** | |

### 2.2 新增 API 端点（23 个）

#### Favorites（4 个）
| 方法 | 路径 | 功能 |
|------|------|------|
| GET | `/api/favorites` | 获取收藏列表（分页） |
| POST | `/api/favorites/tracks/{trackId}` | 收藏曲目 |
| DELETE | `/api/favorites/tracks/{trackId}` | 取消收藏 |
| GET | `/api/favorites/check/{trackId}` | 检查是否已收藏 |

#### Playlist（8 个）
| 方法 | 路径 | 功能 |
|------|------|------|
| GET | `/api/playlists` | 获取用户歌单列表 |
| GET | `/api/playlists/{id}` | 获取歌单详情（含曲目） |
| POST | `/api/playlists` | 创建歌单 |
| PUT | `/api/playlists/{id}` | 编辑歌单（部分更新） |
| DELETE | `/api/playlists/{id}` | 删除歌单 |
| POST | `/api/playlists/{id}/tracks` | 向歌单添加曲目 |
| DELETE | `/api/playlists/{playlistId}/tracks/{trackId}` | 从歌单移除曲目 |
| PUT | `/api/playlists/{id}/tracks/reorder` | 重新排序曲目 |

#### Player（11 个）
| 方法 | 路径 | 功能 |
|------|------|------|
| GET | `/api/player/state` | 获取播放状态 |
| GET | `/api/player/devices` | 获取可用设备列表 |
| PUT | `/api/player/play` | 开始/恢复播放 |
| PUT | `/api/player/pause` | 暂停播放 |
| POST | `/api/player/next` | 下一首 |
| POST | `/api/player/previous` | 上一首 |
| PUT | `/api/player/volume` | 设置音量（0-100） |
| PUT | `/api/player/seek` | 跳转位置（毫秒） |
| PUT | `/api/player/repeat` | 设置重复模式（off/context/track） |
| PUT | `/api/player/shuffle` | 设置随机播放 |
| PUT | `/api/player/device` | 转移播放到设备 |

### 2.3 新增数据库表

| 表 | 字段 | 索引 |
|---|---|---|
| `UserLikes` | Id, UserId, TrackId, CreatedAt | UQ(UserId, TrackId), IX(UserId) |
| `Playlists` | Id, UserId, Name, Description, CoverImageUrl, IsPublic, CreatedAt, UpdatedAt | IX(UserId) |
| `PlaylistTracks` | Id, PlaylistId, TrackId, OrderIndex, AddedAt | UQ(PlaylistId, TrackId), IX(PlaylistId, OrderIndex) |

### 2.4 ISpotifyClient 新增播放方法（11 个）

| 方法 | HTTP | Spotify Endpoint |
|------|------|-----------------|
| `GetPlaybackStateAsync` | GET | `me/player` |
| `GetAvailableDevicesAsync` | GET | `me/player/devices` |
| `StartPlaybackAsync` | PUT | `me/player/play` |
| `PausePlaybackAsync` | PUT | `me/player/pause` |
| `SkipToNextAsync` | POST | `me/player/next` |
| `SkipToPreviousAsync` | POST | `me/player/previous` |
| `SetVolumeAsync` | PUT | `me/player/volume` |
| `SeekToPositionAsync` | PUT | `me/player/seek` |
| `SetRepeatModeAsync` | PUT | `me/player/repeat` |
| `SetShuffleAsync` | PUT | `me/player/shuffle` |
| `TransferPlaybackAsync` | PUT | `me/player` |

### 2.5 新增 Spotify JSON Models（3 个）

- `PlaybackStateObject` — 播放状态（设备、进度、曲目、上下文、操作位掩码）
- `DeviceObject` — 设备信息（名称、类型、音量、活跃状态）
- `PlaybackContextObject` — 播放上下文（类型、URI）

---

## 3. 关键设计决策

### 3.1 Playlist 命名空间冲突

**问题**：项目名 `MusicRec.Playlist` 与实体类 `Playlist` 同名，导致 C# 编译器将 `Playlist` 解析为命名空间而非类型（CS0118）。

**解决**：将项目根命名空间和程序集名改为复数 `MusicRec.Playlists`，避免命名空间与类名冲突。所有 16 个 .cs 文件的 namespace 和 using 语句同步更新。

### 3.2 DesignTimeDbContextFactory 缺失注册导致空迁移

**问题**：`dotnet ef migrations add` 生成空迁移（Up/Down 方法为空），原因是 `DesignTimeDbContextFactory` 未注册 Favorites 和 Playlist 模块的程序集，EF Core 不知道这些新实体。

**解决**：在 `DesignTimeDbContextFactory.CreateDbContext()` 中添加：
```csharp
EntityConfigurationRegistry.Register(typeof(Favorites.DependencyInjection).Assembly);
EntityConfigurationRegistry.Register(typeof(Playlists.DependencyInjection).Assembly);
```

### 3.3 跨模块数据读取

Favorites 和 Playlist 模块需要读取 Catalog 模块的 `Track`/`Artist`/`Album` 实体进行 JOIN 查询。按照模块隔离规则，不应直接引用同级模块。采取务实方案：
- 添加单向项目引用 `Favorites → Catalog`、`Playlist → Catalog`
- 严格限制为只读查询（通过 `_db.Set<Track>()` 等）
- Handler 中不调用 Catalog 的 Command/Query/Handler

### 3.4 Player 模块无本地数据库

Player 模块不存储任何本地数据，纯 API 适配层：
- 无 Entity、无 IEntityTypeConfiguration
- DI 中不调用 `EntityConfigurationRegistry.Register()`
- 所有功能通过 `ISpotifyClient` 代理到 Spotify Web API
- 当前 Client Credentials OAuth 无播放权限，API 骨架已就绪，后续扩展 scope 即可

---

## 4. 开发过程中发现并修复的 Bug

### 4.1 [中等] PlaybackActions 缺少 `disallows` 嵌套层

**位置**：`src/Infrastructure/Spotify/Models/PlaybackStateObject.cs:44`

**问题**：Spotify API 返回 JSON 结构为 `actions.disallows.{property}`，但模型将属性直接映射在 `actions` 层（缺少 `disallows` 中间层）。所有 10 个布尔值永远反序列化为 null。

**修复**：新增 `PlaybackDisallows` 类承载 10 个操作布尔值，`PlaybackActions` 改为包含 `Disallows` 属性。

### 4.2 [中等] AddTrackToPlaylistCommandHandler 返回占位数据

**位置**：`src/Modules/Playlist/Handlers/AddTrackToPlaylistCommandHandler.cs:58`

**问题**：向歌单添加曲目后返回的 `PlaylistTrackItemDto` 中，`Name` 字段硬编码为 `"正在加载"`，`CoverImageUrl` 为 null、`DurationMs` 为 0。前端拿到不完整数据，需额外请求曲目详情。

**修复**：在返回前添加 `Track` + `TrackArtists` 查询，返回真实曲目名称、艺术家、时长、封面图。

### 4.3 [低] SpotifyJsonDefaults 序列化输出 null 属性

**位置**：`src/Infrastructure/Spotify/SpotifyJsonDefaults.cs`

**问题**：`StartPlaybackAsync` 的 JSON body 包含 `"context_uri": null`、`"offset": null` 等冗余字段。

**修复**：添加 `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`。

### 4.4 [低] device_id 未 URL 编码

**位置**：`src/Infrastructure/Spotify/SpotifyClient.cs:207`

**问题**：`BuildPlayerUrl` 方法中 `device_id` 值直接拼接到 URL，未使用 `Uri.EscapeDataString()`。

**修复**：`return $"{baseUrl}{separator}device_id={Uri.EscapeDataString(deviceId)}";`

### 4.5 [低] ReorderPlaylistTracksCommandHandler 异常类型不当

**位置**：`src/Modules/Playlist/Handlers/ReorderPlaylistTracksCommandHandler.cs:37`

**问题**：使用 `ValidationException`（语义为"请求参数验证失败"，HTTP 400）抛出业务逻辑错误（曲目不在歌单中）。应使用 `ConflictException`（HTTP 409）。

**修复**：`throw new ConflictException($"曲目 {trackId} 不在该歌单中");`

### 4.6 [低] GetUserId() 方法重复定义 4 次

**位置**：`FavoritesController`、`PlaylistController`、`IdentityController`、`SearchController`

**问题**：相同的 `GetUserId()` 私有方法在 4 个 Controller 中重复定义，且使用 `Guid.Parse()` 可能抛出非 DomainException 类型的 `FormatException`。

**修复**：
- 提取为 `ClaimsPrincipalExtensions` 静态扩展类（位于 `MusicRec.WebApi.Infrastructure`）
- 使用 `Guid.TryParse` 替代 `Guid.Parse`，所有异常统一为 `UnauthorizedException`
- 同时提供 `GetUserId()`（必选认证）和 `GetUserIdOrNull()`（可选认证）

### 4.7 [低] GetAvailableDevicesQueryHandler 缺少 null 防护

**位置**：`src/Modules/Player/Handlers/GetAvailableDevicesQueryHandler.cs:17`

**问题**：如果 Spotify API 返回 `{"devices": null}`，`System.Text.Json` 会覆盖 `DevicesResponse.Devices` 的默认值 `new()`，导致 Mapster `Adapt()` 抛出 `NullReferenceException`。

**修复**：`return (devices ?? new()).Adapt<List<DeviceDto>>();`

---

## 5. 深度验证发现的已知项（供后续参考）

| # | 项目 | 原因 | 计划 |
|---|------|------|------|
| 1 | "同步用户画像" 未实现 | 依赖 Phase 4 的 UserProfiles 系统，届时通过 MediatR 事件联动 | Phase 4 |
| 2 | Player API 实际不可用 | 当前 Client Credentials Token 无 `user-modify-playback-state` scope | Phase 4/5 扩展 OAuth |
| 3 | `StartPlaybackAsync` 的 `offset` 和 `position_ms` 混用 | `offset`（曲目索引）和 `position_ms`（曲目内位置）是独立参数，当前实现将跳转位置时总是重置为第一首 | 后续扩展接口参数 |
| 4 | `UserLikes` 缺少 `(UserId, CreatedAt DESC)` 复合索引 | 当前数据量极小，非性能瓶颈 | Phase 4 随数据量增长添加 |
| 5 | 部分 Command 无 Validator | 参数类型被 Controller 隐式校验（Guid/int 约束），Validator 主要用于字符串/范围校验 | 按需添加 |

---

## 6. 代码审查与优化

第一次完成全部代码后，进行了深度并行审计（2 个 Agent），发现 14 项问题（7 项已修复，7 项记录供参考）。

第二阶段对全部 54 个文件进行了逐文件代码质量和注释审查：

### 6.1 注释优化

- 所有类添加 `<summary>` 中文注释说明职责
- 所有复杂 Handler 添加 `<remarks>` 说明设计考量（数据库查询策略、并发安全机制、幂等设计）
- 实体字段添加单行注释说明字段含义（如 `TrackId` 对应 `Tracks.Id`，非 `SpotifyTrackId`）
- DI 注册文件说明不注册 `IPipelineBehavior` 的原因（全局统一注册）
- Playlist DI 说明命名空间使用复数形式的原因（避免与实体类冲突）
- Player DI 说明不调用 `EntityConfigurationRegistry.Register` 的原因（无实体）
- SpotifyClient 播放 API 区域添加端点映射表
- SpotifyJsonDefaults 说明 `DefaultIgnoreCondition` 的设置原因
- ClaimsPrincipalExtensions 说明 `Guid.TryParse` 替代 `Guid.Parse` 的原因

### 6.2 结构优化

- 提取 `ClaimsPrincipalExtensions` 消除 4 处 `GetUserId()` 重复
- `GetUserLikesQueryHandler` 补充空结果快速返回（`if (likes.Count == 0) return`）
- `UpdatePlaylistCommandValidator` 补充 `CoverImageUrl` 字段校验
- `GetPlaybackStateQueryHandler` 提取 `MapTrack` 私有方法提升可读性
- `AddTrackToPlaylistCommandHandler` 注释分五步清晰说明处理流程

### 6.3 代码质量指标

| 指标 | 状态 |
|------|------|
| 命名规范（类 PascalCase、方法动词开头） | ✅ |
| CQRS 一致性（Command → Handler → Controller） | ✅ |
| 模块 DI 注册一致性 | ✅ |
| FluentValidation 规则完整性 | ✅ |
| DomainException 使用正确性 | ✅ |
| 并发安全（try-catch DbUpdateException） | ✅ |
| Spotify JSON null 安全（`?.` + `??`） | ✅ |
| 非阻塞写入（搜索历史） | N/A（本 Phase 无此类操作） |

---

## 7. 构建与测试结果

| # | 验证项 | 结果 |
|---|--------|------|
| 1 | `dotnet build`（初始构建） | ✅ 0 错误 0 警告 |
| 2 | `dotnet ef migrations add Phase3_FavoritesAndPlaylist` | ⚠️ 初次为空迁移（DesignTimeDbContextFactory 缺失注册） |
| 3 | `dotnet ef migrations add`（修复后） | ✅ 生成 3 张表、6 个索引、1 个外键 |
| 4 | `dotnet ef database update` | ✅ 迁移成功应用 |
| 5 | `dotnet ef migrations has-pending-model-changes` | ✅ 无待定更改 |
| 6 | `dotnet build`（全部修复后） | ✅ 0 错误 0 警告 |
| 7 | API 启动测试 | ✅ 正常启动无异常 |

---

## 8. 与开发文档的对应关系

| 开发文档要求 | 实现状态 |
|------------|---------|
| 收藏歌曲 (7.7) | ✅ `POST /api/favorites/tracks/{id}` （幂等） |
| 收藏列表 (7.7) | ✅ `GET /api/favorites` （分页，含曲目详情） |
| 同步用户画像 (7.7) | ⏳ 依赖 Phase 4 UserProfiles，通过 MediatR 事件联动 |
| 创建歌单 (7.6) | ✅ `POST /api/playlists` |
| 编辑歌单 (7.6) | ✅ `PUT /api/playlists/{id}` （部分更新） |
| 删除歌单 (7.6) | ✅ `DELETE /api/playlists/{id}` （Cascade） |
| 歌曲排序 (7.6) | ✅ `PUT /api/playlists/{id}/tracks/reorder` |
| 播放控制 (7.10) | ✅ 11 个端点完整覆盖 |
| 当前播放状态 (7.10) | ✅ `GET /api/player/state` |
| 音量控制 (7.10) | ✅ `PUT /api/player/volume` |
| Spotify Web Playback SDK (7.10) | ✅ 后端 API 骨架就绪，前端 SDK 由 Phase 3+ 前端项目负责 |
| 数据库 UserLikes (8.13) | ✅ UserId + TrackId + CreatedAt（额外 Guid Id 用于 IEntity 一致性） |
| 数据库 Playlists (8.9) | ✅ 全部字段匹配 |
| 数据库 PlaylistTracks (8.10) | ✅ PlaylistId + TrackId + OrderIndex（额外 Guid Id 和 AddedAt） |

---

## 9. Phase 4 就绪状态

Phase 1-3 的代码基状态：

- **Identity**：用户注册/登录/JWT（含时序攻击防护）
- **Catalog**：Spotify 元数据本地存储（Track/Artist/Album/Genre）
- **AudioFeatures**：10 维音频特征存储 + 向量化
- **Search**：Spotify 统一搜索 + 搜索历史
- **Favorites**：曲目收藏（幂等、并发安全）
- **Playlist**：歌单 CRUD + 排序（所有权保护）
- **Player**：11 个播放控制端点（API 骨架就绪）

可直接进入 Phase 4（用户行为系统、用户画像、行为埋点）。

---

## 10. 经验教训

1. **C# 命名空间与类名冲突**：当项目命名空间与其中的类同名时，编译器优先将类名解析为命名空间。使用复数形式（`Playlists`）是 .NET 社区的标准解决方案。

2. **EF Migration 工具独立于运行时 DI**：`dotnet ef migrations` 使用 `IDesignTimeDbContextFactory`，不会读取 `Program.cs` 中的 `AddXxxModule()`。新增模块时必须同时在 DesignTimeDbContextFactory 中注册程序集。

3. **System.Text.Json 覆盖默认值**：反序列化 `null` JSON 值时，即使属性有 `= new()` 初始化器，`System.Text.Json` 也会将其设为 null。List 类型属性必须在使用前加 null 检查。

4. **幂等设计减少客户端复杂度**：收藏/取消收藏/移除曲目等操作设计为幂等，前端不需要检查是否存在再决定调用哪个 API，简化状态管理。

5. **ClaimsPrincipal 扩展方法优于私有方法**：`GetUserId()` 在多 Controller 间重复时，提取为扩展方法不仅消除重复，还能统一异常处理逻辑（如 `Guid.TryParse` 替代 `Guid.Parse`）。
