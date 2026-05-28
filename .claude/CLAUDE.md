# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

基于 .NET 平台的个性化音乐推荐系统 — 通过 Spotify Web API 获取歌曲与音频特征，实现混合推荐算法。后端 .NET 9 + C# 13，前端 Next.js 16 + React 19 + TypeScript，数据库 SQL Server `.\SQLEXPRESS` / `MusicRecDb`。Phase 1-6 全部完成，10 个后端模块 + 前端 8 页面 + Redis 双级缓存。

> 音频特征 API 迁移方案详见 `audio-features-api-migration.md`。当前使用种子数据生成器（`POST /api/recommendations/seed`，23 个流派模板）模拟音频特征。

## 构建与运行

```bash
# 还原 + 构建
dotnet restore
dotnet build

# 运行后端
dotnet run --project src/Web/MusicRec.WebApi/MusicRec.WebApi.csproj

# EF Core 迁移（ -p 指向 DbContext 所在项目， -s 指向启动项目）
dotnet ef migrations add <Name> -p src/BuildingBlocks/Infrastructure -s src/Web/MusicRec.WebApi
dotnet ef database update -p src/BuildingBlocks/Infrastructure -s src/Web/MusicRec.WebApi

# 前端
cd frontend && npm install && npm run dev

# 前端构建
cd frontend && npm run build
```

## 架构：模块化单体 (Modular Monolith)

```
src/
├── Web/MusicRec.WebApi/          # API 入口（Controller、中间件、Program.cs）
├── Modules/
│   ├── Identity/                 # 用户模块（注册/登录/JWT/资料）
│   ├── Catalog/                  # 音乐目录（Track/Artist/Album/Genre 本地存储）
│   ├── AudioFeatures/            # 音频特征（10 维度 + ToVector() 向量化）
│   ├── Search/                   # 统一搜索（委托 Spotify API + 搜索历史）
│   ├── Favorites/                # 收藏系统（UserLikes 幂等 CRUD）
│   ├── Playlist/                 # 歌单系统（CRUD + 曲目排序 + 所有权保护）
│   ├── Player/                   # 播放控制（纯 API 适配层，无本地表）
│   ├── UserBehavior/             # 用户行为追踪（播放/跳过/事件 + 用户画像）
│   ├── Recommendation/           # 推荐算法（混合推荐 + 相似曲目 + 种子数据）
│   └── Discovery/                # 探索推荐（冷启动 + 70/20/10 探索分配）
├── BuildingBlocks/
│   ├── Shared/                   # ApiResponse、异常、验证管道、密码哈希、ICacheService、CacheKeys
│   ├── Abstractions/             # IEntity、IPasswordHasher
│   ├── Contracts/                # 跨模块 MediatR 事件
│   └── Infrastructure/           # MusicRecDbContext、EntityConfigurationRegistry、HybridCacheService (L1+L2)
├── Infrastructure/Spotify/       # Spotify API 适配器（OAuth + Token 缓存 + Polly 重试）
├── frontend/                     # Next.js 前端（8 页面 + 7 API 模块 + 2 Zustand Store）
```

## 核心架构规则

1. **模块隔离**：`Modules/X` 禁止直接引用 `Modules/Y`。跨模块通信只能通过 `BuildingBlocks/Contracts` 的事件（MediatR `INotification`）。
2. **项目引用单向**：WebApi → Modules + Infrastructure；Modules → BuildingBlocks + Infrastructure/Spotify；BuildingBlocks 之间 Shared → Abstractions。不可反向。
3. **CQRS**：Controller 只注入 `ISender`，调用 `_sender.Send(command/query)`。业务逻辑全部在 MediatR Handler 中。
4. **验证**：每个 Command 必须有对应的 FluentValidation Validator，通过 `ValidationBehavior`（IPipelineBehavior）自动在 Handler 前执行。**ValidationBehavior 在 Program.cs 中全局注册一次**（禁止各模块重复注册，否则验证会执行 N 次）。
5. **对象映射**：Entity ↔ DTO 使用 `entity.Adapt<Dto>()`（Mapster），禁止手写字段赋值映射。复杂映射通过 `TypeAdapterConfig` 注册（参考 `AudioFeatureMapping.cs`）。
6. **异常**：抛出自定义 `DomainException` 子类（`NotFoundException`/`ValidationException`/`UnauthorizedException`/`ConflictException`），由 `GlobalExceptionMiddleware` 统一捕获并输出 `ApiResponse`。
7. **统一响应**：所有 API 返回 `ApiResponse<T>` 或 `ApiResponse`，格式 `{ success, data, message, errors }`。JWT 401/403 也通过 `JwtBearerEvents` 返回此格式。
8. **单 DbContext**：`MusicRecDbContext` 位于 `BuildingBlocks/Infrastructure`。各模块实体的 `IEntityTypeConfiguration<T>` 通过 `EntityConfigurationRegistry.Register(assembly)` 注册到 DbContext。
9. **密码安全**：`IPasswordHasher`（PBKDF2-SHA256，100K 迭代）注入 Handler 使用，禁止模块自行实现哈希逻辑。登录验证必须用非短求值 `|`（非 `||`）+ 虚拟哈希，确保用户不存在时也执行 PBKDF2，防止时序攻击枚举邮箱。
10. **并发安全**：数据库唯一索引是并发重复检测的最后防线。`SaveChangesAsync` 需 `try-catch DbUpdateException`（检查 SQL 错误号 2601/2627），转换为对应的 DomainException（如 ConflictException），而非让 500 错误泄漏。
11. **Spotify JSON 空值防护**：Spotify API 对本地文件/特殊曲目返回 `"album": null`、`"artists": null`。所有访问嵌套对象的映射代码必须使用 `?.` 空值传播 + `??` 默认值回退，禁止直接访问可能为 null 的嵌套属性。
12. **非关键写入不阻塞主流程**：搜索历史等辅助数据的写入失败不应阻塞核心功能（如搜索）。用 `try-catch` 包裹，失败静默吞掉。
13. **规则优先级**：`.cursorrules` > CLAUDE.md。若两者冲突，以 `.cursorrules` 为准。
14. **命名空间与类名冲突**：项目命名空间不得与其中的实体类同名（C# 编译器会优先将类名解析为命名空间）。典型解决：使用复数命名空间（如 `MusicRec.Playlists` 而非 `MusicRec.Playlist`），或在 csproj 中显式设置 `<RootNamespace>`。
15. **DesignTimeDbContextFactory 注册**：`dotnet ef migrations` 使用 `IDesignTimeDbContextFactory`，不读取 `Program.cs` 中的 `AddXxxModule()`。新增模块时必须在 `DesignTimeDbContextFactory.CreateDbContext()` 中同步调用 `EntityConfigurationRegistry.Register(typeof(Xxx.DependencyInjection).Assembly)`，否则生成的迁移将为空。
16. **Player 模块模式**：纯 API 适配层（无实体、无 EF 配置、不调用 `EntityConfigurationRegistry.Register()`），所有功能通过 `ISpotifyClient` 代理到外部 API。DI 仅需 `AddMediatR` + `AddValidatorsFromAssembly`。
20. **零实体模块模式**：Recommendation、Discovery 模块无自有实体/表，所有结果在内存中计算。DI 中 `EntityConfigurationRegistry.Register()` 调用无害（程序集无配置时为空操作）。跨模块只读访问其他模块实体遵循规则 17。
17. **跨模块只读数据访问**：Favorites/Playlist 需读取 Catalog 的 Track/Artist 实体进行 JOIN 查询。允许单向项目引用 + 严格限制为只读查询（通过共享 `_db.Set<T>()` 操作），禁止调用其他模块的 Command/Query/Handler。
18. **幂等写操作**：收藏/取消收藏/添加曲目/移除曲目等操作应设计为幂等——重复操作不报错（已收藏→返回已有记录，未收藏删除→静默成功）。减少客户端状态判断复杂度。
19. **所有权验证**：歌单/收藏等用户私有资源的写操作必须在 Handler 中验证 `UserId == 当前用户`，非所有者抛 `UnauthorizedException`。Controller 层不应承担此逻辑。
21. **Catalog → AudioFeatures 跨模块引用**：曲目详情页需展示音频特征，Catalog 模块允许单向只读引用 AudioFeatures 模块（`<ProjectReference>`）。仅限 `GetTrackQueryHandler` 中查询 `TrackAudioFeature`（通过 `SpotifyTrackId` 业务键关联），禁止调用 AudioFeatures 模块的 Handler/Service。`TrackDto.AudioFeatures` 为可选参数（默认 null），导入等其他场景不填充。

## 关键设计模式

### EntityConfigurationRegistry — 解耦模块实体与 DbContext

模块的 `DependencyInjection.AddXxxModule()` 中调用 `EntityConfigurationRegistry.Register(typeof(DependencyInjection).Assembly)`，注册本模块的 `IEntityTypeConfiguration<T>` 程序集。`MusicRecDbContext.OnModelCreating` 遍历已注册程序集加载配置。设计时工厂（`DesignTimeDbContextFactory`，位于 WebApi 项目）也需调用 Register。

### Spotify 认证：DelegatingHandler 模式

`SpotifyAuthHandler`（DelegatingHandler）在每次 HTTP 请求前通过 `HttpRequestMessage.Headers` 注入 Bearer 令牌。比在 `HttpClient.DefaultRequestHeaders` 上设置更安全——避免多线程竞争。通过 `AddHttpMessageHandler<SpotifyAuthHandler>()` 注册到 HTTP 管道。

### Spotify Token 缓存

`SpotifyTokenStore` 使用 `IMemoryCache` 缓存 Client Credentials 令牌 3500 秒（提前 100 秒过期，留缓冲）。所有 API 调用通过 `ISpotifyClient` 接口（8 个方法），在 `SpotifyClient` 中实现，Polly 指数退避重试策略（5 次，处理 429/5xx）。

### Spotify JSON 映射

所有 Spotify API 响应的 Model 使用 `[JsonPropertyName("snake_case")]` + `JsonNamingPolicy.SnakeCaseLower` 双重保障。共享选项在 `SpotifyJsonDefaults.Options` 单例中。

### 跨模块事件

`MusicRec.Contracts.Events` 中定义 `record XxxEvent : INotification`。发布方 Handler 注入 `IPublisher` 调用 `_publisher.Publish(event)`。订阅方模块在自身的 Handler 中实现 `INotificationHandler<TEvent>`。

### Cache-Aside 导入模式

Catalog/AudioFeatures 模块的 Import Handler 遵循 Cache-Aside：先查本地 DB，命中直接返回；未命中调用 Spotify API，写入 DB 后返回。降低 API 调用量。

### ClaimsPrincipal 扩展方法

`ClaimsPrincipalExtensions`（位于 `WebApi/Infrastructure/`）统一从 JWT `sub` claim 提取 `UserId`。提供 `GetUserId()`（必选认证）和 `GetUserIdOrNull()`（可选认证）。使用 `Guid.TryParse` 而非 `Guid.Parse`，确保异常统一为 `UnauthorizedException`。所有 Controller 通过 `User.GetUserId()` 调用，禁止各 Controller 定义私有 `GetUserId()` 方法。

### 跨模块只读数据访问

Favorites/Playlist 模块通过共享 `MusicRecDbContext` 的 `_db.Set<Track>()` 查询 Catalog 实体进行 JOIN。允许单向项目引用（`.csproj` 中 `<ProjectReference>`），但严格禁止调用其他模块的 Handler/Service。此模式适用于需要"读取其他模块已有数据但不修改"的场景。

### Player 模块（零实体模式）

无本地数据库表的纯 API 适配模块模板：
- DI 中不调用 `EntityConfigurationRegistry.Register()`
- 无需 `DesignTimeDbContextFactory` 注册（无实体配置）
- Handler 仅注入 `ISpotifyClient`，直接代理外部 API
- 适用于任何"纯代理外部服务"的模块

### 双级缓存 (L1 + L2 Hybrid Cache)

`HybridCacheService` 实现 `ICacheService`，位于 `BuildingBlocks/Infrastructure/Caching/`：

- **L1**：`IMemoryCache`（~100ns），**L2**：Redis `StackExchange.Redis`（~1ms）
- **自动降级**：Redis 连接失败或未配置（`appsettings.json` 中 `RedisConnectionString: null`）→ 纯 L1 模式，系统正常运行
- **缓存击穿保护**：`ConcurrentDictionary<string, SemaphoreSlim>` 防止同一 Key 大量并发穿透
- **前缀失效**：L1 字典匹配 + L2 Redis SCAN，用于用户画像更新后批量清除推荐缓存
- **Cache-Aside 扩展**：`GetOrCreateAsync<T>(key, factory)` — 命中返回，未命中调用工厂并写入
- **集中式键管理**：所有缓存键通过 `CacheKeys` 静态类生成，禁止硬编码字符串
- **接入范围**：4 个推荐/发现 Handler（GetRecommendations、GetSimilarTracks、GetDiscovery、GetColdStart）
- **TTL 策略**：推荐 3min / 相似曲目 30min / 探索 10min / 冷启动 1h / 目录数据 1h

### 前端架构 (Next.js App Router)

```
frontend/src/
├── app/                    # 8 个页面路由（App Router），每个文件夹 = 一个路由
│   ├── page.tsx            # /          首页（已登录→推荐，未登录→冷启动）
│   ├── login/              # /login     登录
│   ├── register/           # /register  注册
│   ├── explore/            # /explore   探索推荐
│   ├── search/             # /search    搜索
│   ├── favorites/          # /favorites 收藏
│   ├── playlists/          # /playlists 歌单
│   └── tracks/[id]/        # /tracks/:id 曲目详情（10 维音频特征 + 相似曲目）
├── components/
│   ├── providers.tsx       # QueryClient + AuthInitializer（TanStack Query 全局配置）
│   └── layout/             # MainLayout / Sidebar / BottomPlayer（Spotify 三栏布局）
├── lib/
│   ├── api-client.ts       # Axios 实例（JWT 拦截 + 响应解包 + 15s 超时）
│   └── api/                # 7 个 API 模块（auth/catalog/discovery/favorites/playlists/recommendations/search）
├── store/
│   ├── auth-store.ts       # Zustand — 用户认证（login/register/logout + Token localStorage 持久化）
│   └── ui-store.ts         # Zustand — UI 状态（侧边栏折叠等）
└── types/api.ts            # 所有 TypeScript 类型（与后端 DTO 一致）
```

**前端架构规则**：
- **组件职责**：页面 (`app/*/page.tsx`) 负责数据获取，展示组件可放 `components/`，跨页面共享
- **状态管理**：认证状态和 UI 状态分离到独立 Store，避免不相关状态变化引起重渲染
- **API 调用**：所有请求通过 `api-client.ts` 的 Axios 实例，禁止页面中直接 `fetch()` 或 `axios.`
- **数据获取**：TanStack Query `useQuery` / `useMutation`，禁止在 `useEffect` 中手写数据获取
- **认证检查**：`AuthInitializer`（`providers.tsx`）在应用启动时从 localStorage 恢复 Token 并验证
- **动画**：Framer Motion，页面级动画（`initial/animate`）+ 交互动画（`whileHover`）+ 骨架屏（`animate-pulse`）
- **API 地址配置**：`api-client.ts` 中 `baseURL` 指向后端（默认 `http://localhost:5000`），生产通过环境变量覆盖

## 技术栈约束

| 层 | 技术 | 版本 | 禁止 |
|---|---|---|---|
| SDK | .NET 9 | 9.0.314 (global.json 锁定) | .NET 10、Preview |
| 数据库 | SQL Server `.\SQLEXPRESS`，库 `MusicRecDb` | — | 手写 SQL |
| ORM | EF Core 9 | 9.0.5 | 其他 ORM |
| 消息 | MediatR | 12.5.0 | — |
| 验证 | FluentValidation | 11.11.0 | Handler 内手写 if 校验 |
| 映射 | Mapster | 7.4.0 | 手动 AutoMapper 配置 |
| 日志 | Serilog | 9.0.0 | — |
| 认证 | JWT Bearer | 9.0.5 | Session/Cookie |
| HTTP | IHttpClientFactory + Polly | 9.0.5 | 直接 new HttpClient() |
| 缓存 | StackExchange.Redis + IMemoryCache | 2.8.31 | 其他 Redis 库 |
| 前端 | Next.js, React, TypeScript | 16.2 / 19.2 / ^5 | 其他框架 |
| 样式 | TailwindCSS（Spotify 风格深色主题） | ^4 | 其他 UI 库 |
| 动画 | Framer Motion | — | 其他动画库 |
| 状态 | Zustand | — | Redux、Context |
| 请求 | TanStack Query | — | 其他 |

## Spotify 集成规范

- 禁止在 Controller/Handler 中直接调用 Spotify API — 必须通过 `ISpotifyClient` 接口
- Token 由 `SpotifyTokenStore` 自动缓存和刷新，业务代码无需关心
- 认证头由 `SpotifyAuthHandler`（DelegatingHandler）自动注入，禁止手写 Authorization 头
- Spotify 元数据应导入本地 DB（Cache-Aside 模式），避免重复 API 调用
- 搜索结果从 Spotify 实时获取（不缓存），导入操作由 Catalog 模块单独处理
- **Spotify JSON 空值安全**：API 对本地文件/特殊曲目可能返回 `"album": null` 或 `"artists": null`。映射 DTO 时必须使用 `?.` + `??` 防护，参考 `SearchMusicCommandHandler.MapTracks`
- `ISpotifyClient` 共 19 个方法（8 个数据获取 + 11 个播放控制）。播放控制端点（`StartPlaybackAsync`、`PausePlaybackAsync` 等）需要 `user-modify-playback-state` / `user-read-playback-state` scope，当前 Client Credentials 模式不支持，API 骨架已就绪
- **音频特征 API 迁移**：Spotify 已废弃 Audio Features 端点。`ISpotifyClient` 中的 `GetAudioFeaturesAsync` / `GetAudioFeaturesBatchAsync` 将在迁移后删除，改为通过 `ISpotifyExtendedClient`（RapidAPI Spotify Extended API）获取。迁移方案详见项目根目录 `audio-features-api-migration.md`。在迁移完成前，不要修改这两个方法的实现
- **System.Text.Json 覆盖默认值**：反序列化 `null` JSON 时，即使属性有 `= new()` 初始化器也会被覆盖为 null。List 类型属性在使用前需加 `?? new()` 防护，参考 `GetAvailableDevicesQueryHandler`
- **播放 API 序列化**：`SpotifyJsonDefaults.Options` 已设置 `DefaultIgnoreCondition = WhenWritingNull`，确保 PUT/POST body 不包含 null 字段。所有序列化使用 `JsonContent.Create(body, options: SpotifyJsonDefaults.Options)`

## 推荐算法

混合推荐公式（Phase 5 已实现）：

```
FinalScore = 0.35 × AudioSimilarity + 0.25 × BehaviorScore
           + 0.15 × GenrePreference + 0.10 × Freshness
           + 0.10 × Diversity + 0.05 × Popularity
```

- 特征向量：`[energy, danceability, valence, tempo, acousticness]`（`TrackAudioFeature.ToVector()` 已实现，Tempo 除以 200 归一化到 [0,1]）
- 相似度计算：余弦相似度（`RecommendationCalculator.CosineSimilarity`）
- **降级公式**（无音频特征时）：`0.30 × GenrePref + 0.25 × Behavior + 0.15 × Popularity + 0.20 × Freshness + 0.10 × 0.5`
- **探索分配**（`GetRecommendationsQueryHandler`）：70% 高分 + 20% 相邻流派 + 10% 随机探索
- **多样性去重**：每位艺术家最多 2 首曲目
- **种子数据**（`POST /api/recommendations/seed`）：23 个流派模板 + 确定性随机生成模拟 `TrackAudioFeature`
- 推荐结果 DTO 必须包含 `Reason` 字段（中文，如"因为你喜欢 {artist}"、"音频特征高度匹配"）
- **推荐缓存**：4 个推荐/发现 Handler 通过 `ICacheService.GetOrCreateAsync` 接入双级缓存，用户画像更新时通过 `UserBehaviorUpdatedEventHandler` 前缀失效

## API 端点

| 方法 | 路径 | 认证 | 功能 |
|------|------|------|------|
| POST | `/api/identity/register` | 无 | 注册（返回 JWT） |
| POST | `/api/identity/login` | 无 | 登录（返回 JWT） |
| GET | `/api/identity/profile` | JWT | 获取/更新用户资料 |
| PUT | `/api/identity/profile` | JWT | 更新昵称/头像 |
| GET | `/api/search?q=&type=&limit=` | 无 | Spotify 统一搜索 |
| GET | `/api/catalog/tracks/{id}` | 无 | 曲目详情 |
| POST | `/api/catalog/tracks/import` | JWT | 从 Spotify 导入曲目 |
| GET | `/api/catalog/artists/{id}` | 无 | 艺术家详情 |
| POST | `/api/catalog/artists/import` | JWT | 从 Spotify 导入艺术家 |
| GET | `/api/catalog/albums/{id}` | 无 | 专辑详情 |
| GET | `/api/favorites` | JWT | 收藏列表（分页） |
| POST | `/api/favorites/tracks/{id}` | JWT | 收藏曲目（幂等） |
| DELETE | `/api/favorites/tracks/{id}` | JWT | 取消收藏（幂等） |
| GET | `/api/favorites/check/{id}` | JWT | 检查收藏状态 |
| GET | `/api/playlists` | JWT | 用户歌单列表 |
| GET | `/api/playlists/{id}` | JWT | 歌单详情（含曲目） |
| POST | `/api/playlists` | JWT | 创建歌单 |
| PUT | `/api/playlists/{id}` | JWT | 编辑歌单（部分更新） |
| DELETE | `/api/playlists/{id}` | JWT | 删除歌单（Cascade） |
| POST | `/api/playlists/{id}/tracks` | JWT | 添加曲目到歌单 |
| DELETE | `/api/playlists/{id}/tracks/{tid}` | JWT | 从歌单移除曲目 |
| PUT | `/api/playlists/{id}/tracks/reorder` | JWT | 重新排序曲目 |
| GET | `/api/player/state` | JWT | 播放状态 |
| GET | `/api/player/devices` | JWT | 可用设备列表 |
| PUT | `/api/player/play` | JWT | 开始/恢复播放 |
| PUT | `/api/player/pause` | JWT | 暂停播放 |
| POST | `/api/player/next` | JWT | 下一首 |
| POST | `/api/player/previous` | JWT | 上一首 |
| PUT | `/api/player/volume` | JWT | 设置音量 |
| PUT | `/api/player/seek` | JWT | 跳转位置 |
| PUT | `/api/player/repeat` | JWT | 重复模式 |
| PUT | `/api/player/shuffle` | JWT | 随机播放 |
| PUT | `/api/player/device` | JWT | 转移播放 |
| POST | `/api/user-behavior/play` | JWT | 记录开始播放 |
| PUT | `/api/user-behavior/play/{id}/finish` | JWT | 记录播放完成 |
| PUT | `/api/user-behavior/play/{id}/skip` | JWT | 记录跳过 |
| POST | `/api/user-behavior/events` | JWT | 记录行为事件（点击/停留） |
| GET | `/api/user-behavior/history` | JWT | 播放历史（分页） |
| GET | `/api/user-behavior/stats` | JWT | 行为统计摘要 |
| GET | `/api/user-behavior/profile` | JWT | 用户偏好画像 |
| POST | `/api/user-behavior/profile/refresh` | JWT | 刷新用户画像 |
| GET | `/api/recommendations?limit=20` | JWT | 个性化推荐列表（含 Score + Reason） |
| GET | `/api/recommendations/similar/{trackId}?limit=10` | JWT | 相似曲目推荐 |
| POST | `/api/recommendations/seed` | JWT | 生成模拟音频特征种子数据 |
| GET | `/api/discovery?limit=20` | JWT | 探索推荐（70/20/10 分配） |
| GET | `/api/discovery/cold-start?limit=20` | 可选 | 冷启动推荐（热度 + 流派多样性） |
| GET | `/health` | 无 | 健康检查（database + redis） |

## 数据库表

| 表 | 模块 | 说明 |
|---|---|---|
| `Users` | Identity | 用户表 |
| `Tracks` | Catalog | 曲目（SpotifyTrackId 唯一） |
| `Artists` | Catalog | 艺术家（SpotifyArtistId 唯一） |
| `Albums` | Catalog | 专辑（SpotifyAlbumId 唯一） |
| `TrackArtists` | Catalog | 曲目-艺术家多对多 |
| `Genres` | Catalog | 流派 |
| `TrackGenres` | Catalog | 曲目-流派多对多 |
| `TrackAudioFeatures` | AudioFeatures | 10 个音频特征维度 |
| `UserSearchHistory` | Search | 搜索历史 |
| `UserLikes` | Favorites | 收藏记录（UQ: UserId+TrackId） |
| `Playlists` | Playlist | 歌单（Cascade 删除 PlaylistTracks） |
| `PlaylistTracks` | Playlist | 歌单曲目关联（UQ: PlaylistId+TrackId） |
| `UserPlayHistory` | UserBehavior | 播放历史（UserId+PlayedAt 索引） |
| `UserBehaviorEvents` | UserBehavior | 行为事件（点击/停留/跳过） |
| `UserProfiles` | UserBehavior | 用户偏好画像（UserId 主键一对一） |

## 开发行为守则

- 每次改动前说明：修改目标、影响文件、风险点、验证方式
- 一次只改一个模块，不一次生成整个项目
- 每次改动后 `dotnet build` 验证 0 错误
- 不擅自增加数据库、改技术栈、改框架版本、改目录结构
- 每次对代码，项目，服务中的bug进行修复都必须在debugLog文件夹下生成一个md文件（命名自定，表达文件内容即可），详细记录问题，位置，修复策略，改动文件，具体时间，改动前后对比，bug的严重等级，bug影响的内容，bug的具体表现，触发情况等各种详细的信息
- 每次对代码进行优化都必须在optimizeLog文件夹下生成一个md文件（命名自定，表达文件内容即可），详细记录优化代码的文件，位置，优化时间，优化前后对比，优化原因，优化策略等各种详细的信息
- 总之任何对代码，项目的改动都需要生成一份说明记录改动的详细信息

<!-- CODEGRAPH_START -->
## CodeGraph

This project has a CodeGraph MCP server (`codegraph_*` tools) configured. CodeGraph is a tree-sitter-parsed knowledge graph of every symbol, edge, and file. Reads are sub-millisecond and return structural information grep cannot.

### When to prefer codegraph over native search

Use codegraph for **structural** questions — what calls what, what would break, where is X defined, what is X's signature. Use native grep/read only for **literal text** queries (string contents, comments, log messages) or after you already have a specific file open.

| Question | Tool |
|---|---|
| "Where is X defined?" / "Find symbol named X" | `codegraph_search` |
| "What calls function Y?" | `codegraph_callers` |
| "What does Y call?" | `codegraph_callees` |
| "How does X reach/become Y? / trace the flow from X to Y" | `codegraph_trace` (one call = the whole path, incl. callback/React/JSX dynamic hops) |
| "What would break if I changed Z?" | `codegraph_impact` |
| "Show me Y's signature / source / docstring" | `codegraph_node` |
| "Give me focused context for a task/area" | `codegraph_context` |
| "See several related symbols' source at once" | `codegraph_explore` |
| "What files exist under path/" | `codegraph_files` |
| "Is the index healthy?" | `codegraph_status` |

### Rules of thumb

- **Answer directly — don't delegate exploration.** For "how does X work" / architecture questions, answer with 2-3 codegraph calls: `codegraph_context` first, then ONE `codegraph_explore` for the source of the symbols it surfaces. For a specific **flow** ("how does X reach Y") start with `codegraph_trace` from→to — one call returns the whole path with dynamic hops bridged — then ONE `codegraph_explore` for the bodies; don't rebuild the path with `codegraph_search` + `codegraph_callers`. Codegraph IS the pre-built index, so spawning a separate file-reading sub-task/agent — or running a grep + read loop — repeats work codegraph already did and costs more for the same answer.
- **Trust codegraph results.** They come from a full AST parse. Do NOT re-verify them with grep — that's slower, less accurate, and wastes context.
- **Don't grep first** when looking up a symbol by name. `codegraph_search` is faster and returns kind + location + signature in one call.
- **Don't chain `codegraph_search` + `codegraph_node`** when you just want context — `codegraph_context` is one call.
- **Don't loop `codegraph_node` over many symbols** — one `codegraph_explore` call returns several symbols' source grouped in a single capped call, while each separate node/Read call re-reads the whole context and costs far more.
- **Index lag — check the staleness banner, don't guess a wait.** When a codegraph response starts with "⚠️ Some files referenced below were edited since the last index sync…", the listed files are pending re-index — Read those specific files for accurate content. Files NOT in that banner are fresh and codegraph is authoritative for them. `codegraph_status` also lists pending files under "Pending sync".

### If `.codegraph/` doesn't exist

The MCP server returns "not initialized." Ask the user: *"I notice this project doesn't have CodeGraph initialized. Want me to run `codegraph init -i` to build the index?"*
<!-- CODEGRAPH_END -->
