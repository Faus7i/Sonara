# Phase 2 开发日志：Spotify 集成 + Catalog + AudioFeatures + Search

> **日期**: 2026-05-21  
> **分支**: main  
> **SDK**: .NET 9.0.314  
> **数据库**: SQL Server `.\SQLEXPRESS` → `MusicRecDb`

---

## 1. 开发目标

按照《基于.NET平台的个性化音乐推荐系统的开发文档》第 17 节 Phase 2 要求，完成以下四项：

| # | 目标 | 说明 |
|---|------|------|
| 1 | Spotify 登录 | Client Credentials OAuth 流程、Token 缓存与自动刷新 |
| 2 | 搜索 | Spotify Web API 统一搜索（track/artist/album），搜索历史记录 |
| 3 | Metadata | Catalog 模块 — 曲目/艺术家/专辑/流派数据本地落库 |
| 4 | Audio Features | 10 个音频特征维度的获取、存储与向量化 |

---

## 2. 交付内容

### 2.1 新增项目（3 个）

```
src/
├── Infrastructure/Spotify/       # MusicRec.Spotify（从空壳填满）
│   ├── ISpotifyClient.cs         # Spotify API 统一接口（8 个方法）
│   ├── SpotifyClient.cs          # HTTP 调用实现
│   ├── SpotifyAuthHandler.cs     # 认证委托处理器（自动注入 Bearer Token）
│   ├── SpotifyTokenStore.cs      # IMemoryCache Token 缓存（3500 秒）
│   ├── SpotifyOptions.cs         # ClientId/ClientSecret 配置 POCO
│   ├── SpotifyJsonDefaults.cs    # SnakeCaseLower JSON 序列化单例
│   ├── DependencyInjection.cs    # AddSpotify() 扩展方法
│   └── Models/                   # 9 个 Spotify API JSON 映射类
│       ├── SpotifyTokenResponse.cs
│       ├── TrackObject.cs        # 含 SimplifiedAlbumObject、ExternalIds、ExternalUrls
│       ├── ArtistObject.cs       # 含 Followers
│       ├── AlbumObject.cs        # 含 SimplifiedTrackObject、CopyrightObject
│       ├── AudioFeaturesObject.cs
│       ├── SearchResponse.cs     # 含 SeveralTracksResponse、SeveralAudioFeaturesResponse
│       ├── ImageObject.cs
│       ├── SimplifiedArtistObject.cs
│       └── PaginatedResponse.cs  # 泛型分页信封
│
├── Modules/Catalog/              # MusicRec.Catalog（音乐元数据）
│   ├── Entities/
│   │   ├── Track.cs
│   │   ├── Artist.cs
│   │   ├── Album.cs
│   │   ├── TrackArtist.cs        # 多对多关联表
│   │   ├── Genre.cs
│   │   └── TrackGenre.cs         # 多对多关联表
│   ├── Configurations/           # 6 个 IEntityTypeConfiguration
│   ├── DTOs/
│   │   ├── TrackDto.cs           # 含 ArtistBriefDto、AlbumBriefDto
│   │   ├── ArtistDto.cs
│   │   ├── AlbumDto.cs           # 含 TrackBriefDto
│   │   └── ImportRequests.cs     # ImportTrackRequest、ImportArtistRequest
│   ├── Commands/
│   │   ├── ImportTrackCommand.cs
│   │   └── ImportArtistCommand.cs
│   ├── Queries/
│   │   ├── GetTrackQuery.cs
│   │   ├── GetArtistQuery.cs
│   │   └── GetAlbumQuery.cs
│   ├── Handlers/                 # 5 个 Handler
│   ├── Validators/               # 2 个 Validator
│   └── DependencyInjection.cs
│
├── Modules/AudioFeatures/        # MusicRec.AudioFeatures（音频特征）
│   ├── Entities/
│   │   └── TrackAudioFeature.cs  # 10 个特征维度 + ToVector()
│   ├── Configurations/
│   ├── DTOs/
│   │   └── AudioFeaturesDto.cs
│   ├── Commands/
│   │   └── ImportAudioFeaturesCommand.cs
│   ├── Queries/
│   │   └── GetAudioFeaturesQuery.cs
│   ├── Handlers/                 # 2 个 Handler
│   ├── Validators/
│   ├── AudioFeatureMapping.cs    # Mapster 映射配置
│   └── DependencyInjection.cs
│
└── Modules/Search/               # MusicRec.Search（统一搜索）
    ├── Entities/
    │   └── SearchHistory.cs
    ├── Configurations/
    ├── DTOs/
    │   └── SearchResultDto.cs    # 含 SearchTrackDto、SearchArtistDto、SearchAlbumDto
    ├── Commands/
    │   └── SearchMusicCommand.cs
    ├── Handlers/
    │   └── SearchMusicCommandHandler.cs
    ├── Validators/
    └── DependencyInjection.cs
```

### 2.2 新增数据库表（8 张）

| 表 | 字段 | 索引 |
|---|---|---|
| `Tracks` | Id, SpotifyTrackId, Name, AlbumId, DurationMs, Popularity, ReleaseDate, CoverImageUrl | SpotifyTrackId 唯一索引 |
| `Artists` | Id, SpotifyArtistId, Name, Genres, ImageUrl, Popularity | SpotifyArtistId 唯一索引 |
| `Albums` | Id, SpotifyAlbumId, Name, ReleaseDate, CoverImageUrl, AlbumType, TotalTracks | SpotifyAlbumId 唯一索引 |
| `TrackArtists` | TrackId, ArtistId | 联合主键 |
| `Genres` | Id, Name | Name 唯一索引 |
| `TrackGenres` | TrackId, GenreId | 联合主键 |
| `TrackAudioFeatures` | Id, SpotifyTrackId + 10 个特征维度 + DurationMs | SpotifyTrackId 唯一索引 |
| `UserSearchHistory` | Id, UserId, Keyword, SearchedAt | UserId 索引、SearchedAt 索引 |

### 2.3 新增 API 端点（7 个）

| 方法 | 路径 | 认证 | 功能 |
|------|------|------|------|
| GET | `/api/search?q=&type=&limit=` | 无 | Spotify 统一搜索 |
| GET | `/api/catalog/tracks/{id}` | 无 | 获取曲目详情 |
| POST | `/api/catalog/tracks/import` | JWT | 从 Spotify 导入曲目 |
| GET | `/api/catalog/artists/{id}` | 无 | 获取艺术家详情 |
| POST | `/api/catalog/artists/import` | JWT | 从 Spotify 导入艺术家 |
| GET | `/api/catalog/albums/{id}` | 无 | 获取专辑详情 |
| GET | `/api/catalog/tracks/{id}/audio-features` | 无 | 获取曲目音频特征 |

### 2.4 新增 NuGet 包

| 项目 | 新增包 | 版本 |
|------|--------|------|
| `MusicRec.Spotify` | `Microsoft.Extensions.Http` | 9.0.5 |
| | `Microsoft.Extensions.Http.Polly` | 9.0.5 |
| | `Microsoft.Extensions.Caching.Memory` | 9.0.5 |
| | `System.Net.Http.Json` | 9.0.5 |

### 2.5 配置变更

**appsettings.json** 新增：
```json
"Spotify": {
    "ClientId": "352220271e18487297cdc08735535e34",
    "ClientSecret": "73ddfe38b99c4efbbfc68f35a90eb5b0"
}
```

---

## 3. 测试结果

### 3.1 构建验证

| # | 项目 | 结果 |
|---|------|------|
| 1 | 初次构建（所有模块创建完成） | ✅ 0 错误 0 警告 |
| 2 | 修复 NuGet 缺失（MemoryCache / System.Net.Http.Json）后重建 | ✅ 0 错误 0 警告 |
| 3 | 审计修复后重建 | ✅ 0 错误 0 警告 |
| 4 | 代码优化后最终构建 | ✅ 0 错误 0 警告 |

### 3.2 数据库迁移

| # | 操作 | 结果 |
|---|------|------|
| 1 | `dotnet ef migrations add Phase2_CatalogSearchAudioFeatures` | ✅ 迁移生成成功 |
| 2 | `dotnet ef database update` | ✅ 8 张新表全部创建 |

### 3.3 API 端点测试

**搜索测试 — "Daft Punk"（type=artist, limit=3）**：
```json
{
  "success": true,
  "data": {
    "artists": [
      {"spotifyArtistId": "4tZwfgrHOc3mvqYlEYSvVi", "name": "Daft Punk"},
      {"spotifyArtistId": "1GhPHrq36VKCY3ucVaZCfo", "name": "The Chemical Brothers"},
      {"spotifyArtistId": "3Mvc8kRgr8LRYYgvFmlZqn", "name": "Ayumi Hamasaki"}
    ]
  }
}
```
✅ Spotify API 调用成功，数据格式正确，图片 URL 完整。

### 3.4 Swagger UI

✅ Swagger 页面可访问（`/swagger/index.html`），新增 7 个端点全部可见且有完整的 `[ProducesResponseType]` 文档。

---

## 4. 审计与修复记录

### 4.1 第一次审计（Phase 2 初始实现，3 个 Agent 并行审计）

**审计范围**: 全部 65+ 个 .cs 源文件  
**发现 6 个问题**，已全部修复。

| # | 严重性 | 问题 | 根因 | 修复方案 |
|---|--------|------|------|----------|
| 1 | **高** | SpotifyClient 线程不安全 | `SetAuthHeaderAsync()` 修改共享的 `HttpClient.DefaultRequestHeaders`，高并发下存在竞争 | 新建 `SpotifyAuthHandler`（DelegatingHandler），每次请求独立设置 Authorization 头，消除 8 处重复调用 |
| 2 | **高** | Token 端点无重试策略 | `"SpotifyAuth"` 命名客户端未配置 Polly，令牌刷新失败立即报错 | 为 `"SpotifyAuth"` 客户端添加 `AddPolicyHandler(GetRetryPolicy())` |
| 3 | **中** | ImportTrackCommandHandler 双 SaveChangesAsync | 先保存 Track 获取 ID，再保存 TrackArtists，两轮数据库往返 | 显式赋 `track.Id = Guid.NewGuid()`，所有数据在一次 SaveChangesAsync 中提交 |
| 4 | **中** | AudioFeatures Handler 逐字段赋值重复 | `AudioFeaturesObject` → `TrackAudioFeature` 的 13 个属性在两个 Handler 中手写赋值 | 新建 `AudioFeatureMapping` Mapster 配置，整个实体由 `.Adapt<TrackAudioFeature>()` 创建 |
| 5 | **低** | ImportTrackRequest/ImportArtistRequest 放 Controller 文件 | 请求 DTO 定义在 `CatalogController.cs` 底部，违反关注点分离 | 移至 `Catalog/DTOs/ImportRequests.cs` |
| 6 | **低** | `SpotifyJsonOptions` 命名不符合项目风格 | "Options" 后缀暗示 IOptions&lt;T&gt; 模式，但实际是静态序列化配置 | 重命名为 `SpotifyJsonDefaults`，与 WebApi 层 `JsonDefaults` 保持一致 |

### 4.2 第二次审计（深度代码质量审查，3 个 Agent 并行审查）

**审查维度**: DRY、线程安全、性能、注释质量、命名一致性

| # | 类别 | 发现 | 优化方案 |
|---|------|------|---------|
| 1 | 线程安全 | `SetAuthHeaderAsync` 修改共享 DefaultRequestHeaders（见第一次审计 #1） | 同上 |
| 2 | DRY | ImportTrack 内双 SaveChangesAsync（见第一次审计 #3） | 同上 |
| 3 | DRY | ImportTrack/ImportArtist 中专辑导入逻辑完全重复 | 提取 `GetOrCreateAlbumAsync`/`GetOrCreateArtistsAsync` 子方法 |
| 4 | 性能 | GetAlbumQueryHandler 两次数据库查询 | 改为单次 `Include/ThenInclude` 查询，内存中去重 |
| 5 | 性能 | `GetAlbumQueryHandler` 使用 `Adapt() + with` 创建两个对象 | 改为直接构造 DTO，一次对象分配 |
| 6 | 代码质量 | `OnRetry` 回调解析 Retry-After 但未使用 | 移除死代码，简化重试策略 |
| 7 | 代码质量 | 多个魔法数字硬编码 | `MaxRetryAttempts=5`、`BackoffBaseSeconds=2`、`TopTracksLimit=10`、`SpotifyBatchLimit=100` 全部提取为命名常量 |
| 8 | 基础设施 | 缺少 `PooledConnectionLifetime` 配置 | 添加 5 分钟连接池生命周期，确保 DNS 变更可感知 |
| 9 | 注释 | 多处只有 WHAT 注释，缺少 WHY | 补充 Tempo/200f 归一化原理、Genres 逗号分隔决策、CacheSeconds 边界逻辑等 WHY 注释 |
| 10 | 命名 | ImportTrackCommandHandler 用完全限定名 `Spotify.Models.TrackObject` | 添加 `using MusicRec.Spotify.Models`，改为 `SimplifiedAlbumObject` 参数类型 |

---

## 5. 代码优化记录

### 5.1 认证线程安全：SpotifyAuthHandler（DelegatingHandler 模式）

**问题**: `SpotifyClient` 中 8 个方法都调用 `SetAuthHeaderAsync(ct)`，该方法修改 `_http.DefaultRequestHeaders.Authorization`。在 ASP.NET Core 中，`HttpClient` 实例被多个请求并发共享，修改 `DefaultRequestHeaders` 会导致竞态条件——一个请求可能使用另一个请求的 Token。

**方案**: 
- 新建 `SpotifyAuthHandler : DelegatingHandler`，重写 `SendAsync`
- 在每次请求的 `HttpRequestMessage` 层面设置 `Authorization` 头（而非 `HttpClient.DefaultRequestHeaders`）
- 通过 `AddHttpMessageHandler<SpotifyAuthHandler>()` 注入 HTTP 管道
- `SpotifyClient` 构造函数不再需要 `SpotifyTokenStore`，方法签名更简洁

**影响**: 消除 8 处重复调用，保证线程安全，SpotifyClient 职责更单一。

### 5.2 消除字段赋值重复：Mapster 映射配置

**问题**: `ImportAudioFeaturesCommandHandler`（48-66 行）和 `GetAudioFeaturesQueryHandler`（38-54 行）各自将 `AudioFeaturesObject` 的 13 个属性逐一赋值给 `TrackAudioFeature`，完全相同的代码出现了两次。

**方案**:
- 新建 `AudioFeatureMapping.Configure()`，注册 Mapster `TypeAdapterConfig<AudioFeaturesObject, TrackAudioFeature>`
- 仅需配置 `SpotifyTrackId ← Id` 的映射，其余同名属性由 Mapster 约定自动映射
- 两个 Handler 改为一行 `af.Adapt<TrackAudioFeature>()`

### 5.3 数据库事务合并：单次 SaveChangesAsync

**问题**: `ImportTrackCommandHandler` 先 `SaveChangesAsync` 获取 Track.Id，再用此 Id 创建 TrackArtist，再次 `SaveChangesAsync`。两次独立的数据库事务增加延迟和失败风险。

**方案**: 在构造 Track 时显式赋 `Id = Guid.NewGuid()`（而非依赖 DbContext 的自动分配），使 TrackArtist 能在同一次 `SaveChangesAsync` 中引用 Track.Id。EF Core 按插入顺序执行，先插入 Track 再插入 TrackArtist。

### 5.4 查询合并：GetAlbumQueryHandler 单次数据库往返

**问题**: 先查 Album（Include Tracks），再根据 Tracks 的 Id 列表查 TrackArtist 并 Include Artist。两次独立的数据库查询和结果集。

**方案**:
```csharp
// 优化前：2 次查询
var album = await _db.Set<Album>().Include(a => a.Tracks).FirstOrDefaultAsync(...);
var trackArtists = await _db.Set<TrackArtist>().Include(ta => ta.Artist)
    .Where(ta => album.Tracks.Select(t => t.Id).Contains(ta.TrackId)).ToListAsync();

// 优化后：1 次查询
var album = await _db.Set<Album>()
    .Include(a => a.Tracks).ThenInclude(t => t.TrackArtists).ThenInclude(ta => ta.Artist)
    .FirstOrDefaultAsync(...);
// 内存中去重：album.Tracks.SelectMany(t => t.TrackArtists).Select(ta => ta.Artist).Distinct()
```

### 5.5 命名规范化

| 旧名称 | 新名称 | 原因 |
|--------|--------|------|
| `SpotifyJsonOptions` | `SpotifyJsonDefaults` | 与 WebApi 层 `JsonDefaults` 风格统一，避免与 `IOptions<T>` 混淆 |
| `SpotifyJsonOptions.Default` | `SpotifyJsonDefaults.Options` | 属性名更清晰地表明这是一个 `JsonSerializerOptions` 实例 |

### 5.6 魔法数字提取

| 位置 | 魔法值 | 常量名 |
|------|--------|--------|
| `DependencyInjection.cs` | `5`（重试次数） | `MaxRetryAttempts` |
| `DependencyInjection.cs` | `2`（退避基数） | `BackoffBaseSeconds` |
| `DependencyInjection.cs` | `1000`（抖动毫秒） | `JitterMaxMs` |
| `ImportArtistCommandHandler.cs` | `10`（热门曲目数） | `TopTracksLimit`（附 WHY：Spotify API 上限） |
| `ImportAudioFeaturesCommandHandler.cs` | `100`（批量上限） | `SpotifyBatchLimit`（附 WHY：API 限制） |

### 5.7 注释优化：从 WHAT 到 WHY

针对以下关键设计决策补充了 WHY 注释：

| 文件 | 注释内容 |
|------|---------|
| `TrackAudioFeature.cs` | `Tempo / 200f` — 归一化到约 [0,1]，与其他 4 个维度量纲一致，保证余弦相似度计算的公平性 |
| `Artist.cs` | `Genres` 用逗号分隔字符串而非多对多表 — Artist 通常只有 1-5 个 genre，独立表得不偿失 |
| `SpotifyTokenStore.cs` | `CacheSeconds = 3500` — 提前 100 秒过期留出缓冲，防边界时刻的 API 调用失败 |
| `SearchMusicCommandHandler.cs` | 搜索不缓存结果 — 由 Catalog 模块的 Import 负责按需落库 |
| `ImportTrackCommandHandler.cs` | Cache-Aside 模式 — 先本地后远程，降低 Spotify API 调用量和延迟 |
| `SpotifyAuthHandler.cs` | DelegatingHandler 替代 Shared Header — 线程安全、消除重复、职责单一 |

---

## 6. 架构决策记录

### ADR-003: DelegatingHandler 实现认证注入

**问题**: `HttpClient.DefaultRequestHeaders` 是实例级可变状态。在 ASP.NET Core 中，`IHttpClientFactory` 管理的 `HttpClient` 可能被多个并发请求共享。直接在 `DefaultRequestHeaders` 上设置 `Authorization` 存在竞态条件。

**方案**: 使用 `DelegatingHandler`（`SpotifyAuthHandler`）在 `HttpRequestMessage` 层面注入 `Authorization` 头。每个请求独立设置，天然线程安全。

**替代方案**: 在每个 `SpotifyClient` 方法中创建新的 `HttpRequestMessage` 并手动设置头。缺点：8 个方法各自重复设置逻辑。

### ADR-004: Artist.Genres 使用逗号分隔字符串

**问题**: 开发文档定义了独立的 `Genres` 和 `TrackGenres` 表，按规范应使用多对多关联。

**方案**: 当前 `Artist.Genres` 存储为逗号分隔字符串（如 "electronic,dance,house"），原因：
1. Artist 的 genre 数量极少（通常 1-5 个）
2. 主要使用场景是展示（搜索结果、艺术家详情页），而非按 genre 过滤
3. 避免额外的 JOIN 查询和关联表维护

**计划**: 如果后续需支持"按流派浏览"功能，再迁移到独立的 Genre + TrackGenre 表。

---

## 7. 已知限制

| # | 限制 | 影响 | 计划 |
|---|------|------|------|
| 1 | 搜索不缓存 Spotify 结果 | 每次搜索都调用 API，消耗配额 | 高频搜索词可考虑 MemoryCache 缓存 |
| 2 | Artist.Genres 存为逗号分隔字符串 | 无法高效按流派过滤/聚合 | 需要时迁移到独立表 |
| 3 | 无分页支持 | 搜索结果最多 50 条，无法翻页 | Phase 3+ 添加 offset/limit 分页 |
| 4 | 音频特征 API 已被 Spotify 标记为弃用 | 未来可能不可用 | 关注 Spotify 开发者公告，备用方案待定 |
| 5 | Spotify ClientId/Secret 明文存储 | 安全隐患（中） | 上线前迁移到 `dotnet user-secrets` 或环境变量 |
| 6 | Import 端点仅需登录即可调用 | 任何注册用户都能触发 Spotify API 调用 | Phase 3 评估是否添加角色限制 |
| 7 | 无 API 调用量监控 | 无法判断是否接近 Spotify 速率限制 | 添加 Serilog 指标记录 |

---

## 8. Phase 3 准备

以下基础设施已就位，可直接开始 Phase 3：

- **Spotify 适配器完整**：认证、搜索、元数据、音频特征全部可用
- **Catalog 模块就绪**：Track/Artist/Album 数据已支持导入和本地查询
- **AudioFeatures 就绪**：特征向量化方法 `ToVector()` 可直接供推荐算法调用
- **Polly 重试 + 令牌缓存**：生产级稳定性保障
- **Swagger 文档完整**：所有端点均有 `[ProducesResponseType]` 注解

### Phase 3 目标

- 收藏系统（Favorites 模块）
- 歌单系统（Playlist 模块）
- 播放系统（Player 模块 — Spotify Web Playback SDK）
- 前端项目初始化（Next.js + TypeScript + TailwindCSS + Shadcn UI）
