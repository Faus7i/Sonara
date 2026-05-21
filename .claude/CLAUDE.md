# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

基于 .NET 平台的个性化音乐推荐系统 — 通过 Spotify Web API 获取歌曲与音频特征，实现混合推荐算法。后端 .NET 9 + C# 13，前端 Next.js + TypeScript（待搭建），数据库 SQL Server `.\SQLEXPRESS` / `MusicRecDb`。

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

# 前端（待搭建）
cd frontend && npm install && npm run dev
```

## 架构：模块化单体 (Modular Monolith)

```
src/
├── Web/MusicRec.WebApi/          # API 入口（Controller、中间件、Program.cs）
├── Modules/
│   ├── Identity/                 # 用户模块（注册/登录/JWT/资料）
│   ├── Catalog/                  # 音乐目录（Track/Artist/Album/Genre 本地存储）
│   ├── AudioFeatures/            # 音频特征（10 维度 + ToVector() 向量化）
│   └── Search/                   # 统一搜索（委托 Spotify API + 搜索历史）
├── BuildingBlocks/
│   ├── Shared/                   # ApiResponse、异常、验证管道、密码哈希
│   ├── Abstractions/             # IEntity、IPasswordHasher
│   ├── Contracts/                # 跨模块 MediatR 事件
│   └── Infrastructure/           # MusicRecDbContext、EntityConfigurationRegistry
└── Infrastructure/Spotify/       # Spotify API 适配器（OAuth + Token 缓存 + Polly 重试）
```

## 核心架构规则

1. **模块隔离**：`Modules/X` 禁止直接引用 `Modules/Y`。跨模块通信只能通过 `BuildingBlocks/Contracts` 的事件（MediatR `INotification`）。
2. **项目引用单向**：WebApi → Modules + Infrastructure；Modules → BuildingBlocks + Infrastructure/Spotify；BuildingBlocks 之间 Shared → Abstractions。不可反向。
3. **CQRS**：Controller 只注入 `ISender`，调用 `_sender.Send(command/query)`。业务逻辑全部在 MediatR Handler 中。
4. **验证**：每个 Command 必须有对应的 FluentValidation Validator，通过 `ValidationBehavior`（IPipelineBehavior）自动在 Handler 前执行。
5. **对象映射**：Entity ↔ DTO 使用 `entity.Adapt<Dto>()`（Mapster），禁止手写字段赋值映射。复杂映射通过 `TypeAdapterConfig` 注册（参考 `AudioFeatureMapping.cs`）。
6. **异常**：抛出自定义 `DomainException` 子类（`NotFoundException`/`ValidationException`/`UnauthorizedException`/`ConflictException`），由 `GlobalExceptionMiddleware` 统一捕获并输出 `ApiResponse`。
7. **统一响应**：所有 API 返回 `ApiResponse<T>` 或 `ApiResponse`，格式 `{ success, data, message, errors }`。JWT 401/403 也通过 `JwtBearerEvents` 返回此格式。
8. **单 DbContext**：`MusicRecDbContext` 位于 `BuildingBlocks/Infrastructure`。各模块实体的 `IEntityTypeConfiguration<T>` 通过 `EntityConfigurationRegistry.Register(assembly)` 注册到 DbContext。
9. **密码安全**：`IPasswordHasher`（PBKDF2-SHA256，100K 迭代）注入 Handler 使用，禁止模块自行实现哈希逻辑。
10. **规则优先级**：`.cursorrules` > CLAUDE.md。若两者冲突，以 `.cursorrules` 为准。

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
| 前端 | Next.js, React, TypeScript | — | 其他框架 |
| 样式 | TailwindCSS + Shadcn UI | — | 其他 UI 库 |
| 状态 | Zustand | — | Redux、Context |
| 请求 | TanStack Query | — | 其他 |

## Spotify 集成规范

- 禁止在 Controller/Handler 中直接调用 Spotify API — 必须通过 `ISpotifyClient` 接口
- Token 由 `SpotifyTokenStore` 自动缓存和刷新，业务代码无需关心
- 认证头由 `SpotifyAuthHandler`（DelegatingHandler）自动注入，禁止手写 Authorization 头
- Spotify 元数据应导入本地 DB（Cache-Aside 模式），避免重复 API 调用
- 搜索结果从 Spotify 实时获取（不缓存），导入操作由 Catalog 模块单独处理

## 推荐算法

混合推荐公式（Phase 4-5 实现）：

```
FinalScore = 0.35 × AudioSimilarity + 0.25 × BehaviorScore
           + 0.15 × GenrePreference + 0.10 × Freshness
           + 0.10 × Diversity + 0.05 × Popularity
```

- 特征向量：`[energy, danceability, valence, tempo, acousticness]`（`TrackAudioFeature.ToVector()` 已实现，Tempo 除以 200 归一化到 [0,1]）
- 相似度计算：余弦相似度 或 欧氏距离
- 推荐结果 DTO 必须包含 `Reason` 字段

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

## 开发行为守则

- 每次改动前说明：修改目标、影响文件、风险点、验证方式
- 一次只改一个模块，不一次生成整个项目
- 每次改动后 `dotnet build` 验证 0 错误
- 不擅自增加数据库、改技术栈、改框架版本、改目录结构
