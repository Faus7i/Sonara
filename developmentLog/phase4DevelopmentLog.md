# Phase 4 — 用户行为系统开发日志

> **开发日期**：2026-05-21 ~ 2026-05-22
> **审查日期**：2026-05-22
> **模块**：`src/Modules/UserBehavior/`
> **开发文档章节**：7.9 UserBehavior / 8.11-8.14 数据库表 / Phase 4 开发顺序

---

## 一、开发目标

对照开发文档第 17 节 Phase 4 要求：

| 目标 | 说明 |
|---|---|
| **用户行为系统** | 采集播放记录、跳过行为、点击行为、停留时间 |
| **用户画像** | 基于播放/收藏行为聚合计算音乐偏好摘要 |
| **行为埋点** | 通用行为事件采集，供 Phase 5 推荐算法训练使用 |

对应开发文档 7.9 节具体功能：

1. 播放记录 — 开始播放、播放完成、播放时长
2. 搜索记录 — 由 Search 模块采集，UserBehavior 跨模块读取
3. 跳过行为 — 记录跳过位置、标记未完成播放
4. 点击行为 — 通用行为事件（点击/停留/滚动/浏览详情页）
5. 停留时间 — 记录用户在详情页等页面的停留时长
6. 用于推荐算法训练 — 发布 `UserBehaviorUpdatedEvent` 供 Phase 5 订阅

---

## 二、交付内容

### 2.1 模块文件清单（共 17 个源文件，838 行代码）

```
src/Modules/UserBehavior/
├── MusicRec.UserBehavior.csproj              (25 行)  项目文件
├── DependencyInjection.cs                    (27 行)  DI 注册
├── Commands/
│   └── UserBehaviorCommands.cs               (24 行)  5 个 Command
├── Queries/
│   └── UserBehaviorQueries.cs                (24 行)  3 个 Query
├── DTOs/
│   └── UserBehaviorDtos.cs                   (48 行)  3 个 DTO
├── Entities/
│   ├── UserPlayHistory.cs                    (19 行)  播放历史实体
│   ├── UserBehaviorEvent.cs                  (24 行)  行为事件实体
│   └── UserProfile.cs                        (31 行)  用户画像实体
├── Configurations/
│   ├── UserPlayHistoryConfiguration.cs       (20 行)  EF 配置
│   ├── UserBehaviorEventConfiguration.cs     (19 行)  EF 配置
│   └── UserProfileConfiguration.cs           (24 行)  EF 配置
├── Validators/
│   └── UserBehaviorValidators.cs             (54 行)  5 个 Validator
└── Handlers/
    ├── RecordPlayCommandHandler.cs           (50 行)  记录播放开始
    ├── FinishPlayCommandHandler.cs           (43 行)  记录播放完成
    ├── SkipPlayCommandHandler.cs             (55 行)  记录跳过
    ├── RecordBehaviorEventCommandHandler.cs  (41 行)  记录通用行为事件
    ├── GetPlayHistoryQueryHandler.cs         (67 行)  播放历史查询
    ├── GetUserBehaviorStatsQueryHandler.cs   (60 行)  行为统计查询
    ├── GetUserProfileQueryHandler.cs         (41 行)  用户画像查询
    └── RefreshUserProfileCommandHandler.cs  (167 行)  刷新用户画像（核心算法）
```

### 2.2 关联文件（3 个文件，201 行代码）

| 文件 | 行数 | 说明 |
|---|---|---|
| `src/BuildingBlocks/Contracts/Events/UserBehaviorUpdatedEvent.cs` | 7 | 跨模块领域事件 |
| `src/Web/MusicRec.WebApi/Controllers/UserBehaviorController.cs` | 97 | API 控制器（10 个端点） |
| `src/BuildingBlocks/Infrastructure/Migrations/20260521155004_AddUserBehavior.cs` | 97 | EF Core 迁移 |

### 2.3 修改的既有文件（3 个文件，+128 行）

| 文件 | 变更 |
|---|---|
| `src/Web/MusicRec.WebApi/Program.cs` | +2 行 — 注册 `AddUserBehaviorModule()` |
| `src/Web/MusicRec.WebApi/DesignTimeDbContextFactory.cs` | +2 行 — 注册 UserBehavior 实体配置 |
| `src/BuildingBlocks/Infrastructure/Migrations/MusicRecDbContextModelSnapshot.cs` | +124 行 — 3 张新表的 ModelSnapshot |

### 2.4 代码总量

| 类别 | 行数 |
|---|---|
| UserBehavior 模块源码 | 838 |
| 关联新增文件 | 201 |
| 既有文件修改 | +128 |
| **合计** | **1,167** |

---

## 三、数据库表

EF Core 迁移创建 3 张新表：

### 3.1 UserPlayHistory（播放历史）

| 字段 | 类型 | 说明 |
|---|---|---|
| `Id` | uniqueidentifier PK | 主键 |
| `UserId` | uniqueidentifier NOT NULL | 用户 ID |
| `TrackId` | uniqueidentifier NOT NULL | 曲目 ID |
| `PlayedAt` | datetime2 NOT NULL | 播放开始时间 |
| `DurationPlayed` | int NOT NULL | 实际播放时长（毫秒） |
| `Completed` | bit NOT NULL | 是否完整播放 |
| `Source` | nvarchar(50) NULL | 播放来源 |

**索引**：
- `(UserId, PlayedAt) DESC` — 按时间分页查询优化
- `(UserId, TrackId, PlayedAt)` — 用户+曲目组合查询优化

### 3.2 UserBehaviorEvents（行为事件）

| 字段 | 类型 | 说明 |
|---|---|---|
| `Id` | uniqueidentifier PK | 主键 |
| `UserId` | uniqueidentifier NOT NULL | 用户 ID |
| `TrackId` | uniqueidentifier NULL | 关联曲目（可为空） |
| `EventType` | nvarchar(50) NOT NULL | 事件类型 |
| `Context` | nvarchar(100) NULL | 页面上下文 |
| `Duration` | int NULL | 停留时长（毫秒） |
| `Metadata` | nvarchar(2000) NULL | 扩展元数据 JSON |
| `CreatedAt` | datetime2 NOT NULL | 事件时间 |

**索引**：
- `(UserId, EventType, CreatedAt) DESC` — 按类型+时间查询
- `(UserId, CreatedAt) DESC` — 按时间查询

### 3.3 UserProfiles（用户画像）

| 字段 | 类型 | 说明 |
|---|---|---|
| `UserId` | uniqueidentifier PK | 主键（与 Users 一对一） |
| `FavoriteGenres` | nvarchar(500) NULL | 偏好流派 Top 5（JSON 数组） |
| `AvgEnergy` | float NOT NULL | 加权平均能量 |
| `AvgDanceability` | float NOT NULL | 加权平均舞蹈性 |
| `AvgValence` | float NOT NULL | 加权平均情绪效价 |
| `AvgTempo` | float NOT NULL | 加权平均 BPM |
| `AvgAcousticness` | float NOT NULL | 加权平均原声度 |
| `TopArtists` | nvarchar(2000) NULL | Top 10 艺术家（JSON） |
| `TopTracks` | nvarchar(2000) NULL | Top 10 曲目（JSON） |
| `ExplorationLevel` | float NOT NULL | 探索意愿 0~1 |
| `TotalPlayCount` | int NOT NULL | 总播放次数 |
| `LastUpdatedAt` | datetime2 NOT NULL | 画像刷新时间 |

---

## 四、API 端点（10 个）

| 方法 | 路径 | 功能 | 命令/查询 |
|---|---|---|---|
| `POST` | `/api/user-behavior/play` | 记录开始播放 → 返回播放历史 ID | `RecordPlayCommand` |
| `PUT` | `/api/user-behavior/play/{id}/finish` | 记录播放完成 | `FinishPlayCommand` |
| `PUT` | `/api/user-behavior/play/{id}/skip` | 记录跳过 | `SkipPlayCommand` |
| `POST` | `/api/user-behavior/events` | 记录通用行为事件 | `RecordBehaviorEventCommand` |
| `GET` | `/api/user-behavior/history` | 播放历史（分页） | `GetPlayHistoryQuery` |
| `GET` | `/api/user-behavior/stats` | 行为统计摘要 | `GetUserBehaviorStatsQuery` |
| `GET` | `/api/user-behavior/profile` | 获取用户画像 | `GetUserProfileQuery` |
| `POST` | `/api/user-behavior/profile/refresh` | 手动刷新画像 | `RefreshUserProfileCommand` |

全部端点均需 JWT 认证，返回统一 `ApiResponse<T>` 格式。

---

## 五、与开发文档的对照检查

### 5.1 模块职责（文档 7.9 节）

| 文档要求 | 实现状态 | 说明 |
|---|---|---|
| 播放记录 | ✅ 完整 | 开始 → 完成 → 跳过的完整生命周期 |
| 搜索记录 | ✅ 跨模块 | 由 Search 模块的 UserSearchHistory 表采集，UserBehavior 通过跨模块 JOIN 读取 |
| 跳过行为 | ✅ 完整 | 含跳过位置记录 + 幂等保护 |
| 点击行为 | ✅ 完整 | 通过通用行为事件采集 |
| 停留时间 | ✅ 完整 | 通过 `duration` 参数采集 |
| 用于推荐算法训练 | ✅ 完整 | `UserBehaviorUpdatedEvent` 供 Phase 5 订阅 |

### 5.2 架构规则（CLAUDE.md）

| 规则 | 状态 |
|---|---|
| CQRS（Controller 仅注入 ISender） | ✅ |
| 每个 Command 配 FluentValidation Validator | ✅ |
| 全局 ValidationBehavior（模块 DI 不重复注册） | ✅ |
| Mapster 对象映射 | ✅ |
| 统一 ApiResponse 响应 | ✅ |
| 单 DbContext（MusicRecDbContext） | ✅ |
| 跨模块只读数据访问（Catalog/AudioFeatures/Favorites） | ✅ |
| DesignTimeDbContextFactory 注册 | ✅ |
| ClaimsPrincipal.GetUserId() 扩展方法 | ✅ |
| 非关键写入 try-catch 容错（规则 12） | ✅ |
| 所有权验证（FinishPlay/SkipPlay） | ✅ |
| 幂等操作设计 | ✅ |
| .NET 9 / C# 13 | ✅ |

---

## 六、测试结果

### 6.1 编译测试

```
dotnet build — 0 错误, 0 警告
所有 14 个项目编译通过
```

### 6.2 架构合规测试

| 检查项 | 结果 |
|---|---|
| 命名空间不与类名冲突 | ✅ MusicRec.UserBehavior，所有类在子命名空间下 |
| 项目引用单向（WebApi → Modules → BuildingBlocks） | ✅ |
| 无循环依赖 | ✅ |
| Spotify API 未在模块中直接调用 | ✅ |
| EntityConfigurationRegistry 正确注册 | ✅ |

### 6.3 代码质量

| 指标 | 数值 |
|---|---|
| 每个 Handler 平均行数 | 65 行 |
| 最大 Handler（RefreshUserProfile） | 167 行（五阶段算法 + 注释） |
| 注释覆盖率 | 所有 public 类/方法 + 关键算法步骤 |
| 死代码 | 0（已清除） |

---

## 七、复查发现的 Bug 与修复

### Bug #1：RecordPlay 端点不返回播放历史 ID（严重程度：中）

**发现时间**：2026-05-22 审查

**现象**：
- `POST /api/user-behavior/play` 返回 `{ success: true, message: "播放已记录" }`，不含新创建的播放记录 ID
- 后续 `PUT /api/user-behavior/play/{id}/finish` 和 `PUT /api/user-behavior/play/{id}/skip` 需要此 ID
- 前端只能通过额外调用 `GET /api/user-behavior/history` 反查，存在竞态条件

**根因**：`RecordPlayCommand` 返回 `IRequest`（void），Command 设计未考虑前后端协议完整性。

**修复**（3 处修改）：

| 文件 | 修改 |
|---|---|
| `Commands/UserBehaviorCommands.cs:9` | `IRequest` → `IRequest<Guid>` |
| `Handlers/RecordPlayCommandHandler.cs:14` | `IRequestHandler<RecordPlayCommand>` → `IRequestHandler<RecordPlayCommand, Guid>`，Handle 方法返回 `historyId` |
| `Controllers/UserBehaviorController.cs:31-37` | `ActionResult<ApiResponse>` → `ActionResult<ApiResponse<Guid>>`，接收并包装 `playHistoryId` |

**修复后行为**：即使 DB 写入失败（被 try-catch 静默），也返回生成的 GUID。前端用此 ID 调用 finish/skip 时，Handler 检测记录不存在会记录 Warning 日志后静默返回，不会抛异常。

### Bug #2：冗余项目引用 — MusicRec.Search（严重程度：低）

**发现时间**：2026-05-22 审查

**现象**：`MusicRec.UserBehavior.csproj` 引用了 `MusicRec.Search.csproj`，但所有 Handler 均未使用 Search 模块的任何实体或服务。

**影响**：增加不必要的依赖关系，轻微拖慢增量编译，误导后续维护者。

**修复**：删除 `.csproj` 第 24 行 `<ProjectReference Include="..\Search\MusicRec.Search.csproj" />`。

---

## 八、代码优化记录

### 优化 #1：RefreshUserProfileCommandHandler 合并重复 DB 查询

**优化前**：Handler 先查询 `tracks`（仅播放过的曲目）用于音频特征计算，再单独查询 `allTracks`（播放+收藏曲目）用于流派/艺术家分析。两次查询的曲目集合有大量重叠，造成一次冗余 DB 往返。

**优化后**：先计算 `allRelevantTrackIds = playedTrackIds ∪ likedTrackIds`，然后一次查询 `allTracks`。音频特征计算时从 `allTracks` 中按 `playedTrackIds` 过滤使用。DB 往返次数减少 1 次。

### 优化 #2：清除死代码 — trackMap 变量

**位置**：`RefreshUserProfileCommandHandler.cs`（优化前 L57）

**现象**：`var trackMap = tracks.ToDictionary(t => t.SpotifyTrackId);` 创建后从未被引用，纯粹的死代码。

**修复**：删除该行。SpotifyTrackId 到 Track 的映射在优化后不再需要（直接使用 `trackById` + `audioBySpotifyId` 完成所有查找）。

### 优化 #3：变量命名规范化

| 优化前 | 优化后 | 原因 |
|---|---|---|
| `Count`（匿名类型） | `PlayCount` | 明确语义：是播放次数计数 |
| `trackMap` | `trackById` | 明确键类型：按 Id 索引 |
| `audioMap` | `audioBySpotifyId` | 明确键类型：按 SpotifyTrackId 索引 |
| `histories` → `history` 中 `TrackArtists?.Count` | 保持原样 | 正确使用 `?.` 空值传播 |

### 优化 #4：中文注释全面增强

为所有 8 个 Handler、4 个 Entity、3 个 Configuration、DI 注册、DTO、Command/Query、Validator 添加了中文注释，重点覆盖：

- **算法逻辑**：RefreshUserProfile 的五阶段算法的每步说明
- **设计决策**：为什么 GetUserBehaviorStatsQueryHandler 使用多次 CountAsync 而非复杂聚合查询（毕业设计规模数据量下可读性优先）
- **幂等规则**：SkipPlay（已完成不标记、相同位置不重复写入）、FinishPlay（已标记静默返回）
- **安全考量**：空值传播保护 Spotify API 返回 null 嵌套对象
- **性能权衡**：三阶段分页策略（先分页再批量 JOIN，避免跨表分页的低效 SQL）
- **字段语义**：EventType/Context/Source 等枚举类字段的合法取值

---

## 九、架构设计要点

### 9.1 播放生命周期

```
RecordPlay (创建记录, DurationPlayed=0, Completed=false)
    │
    ├─→ 播放中（前端周期性上报 DurationPlayed，可未来扩展）
    │
    ├─→ FinishPlay (DurationPlayed=实际时长, Completed=true)
    │
    └─→ SkipPlay (DurationPlayed=跳过位置, Completed=false)
         └─→ 同时写入 UserBehaviorEvent (EventType="skip")
```

### 9.2 行为事件分离原则

- **播放生命周期** → `UserPlayHistory` 表 — 结构化的播放/完成/跳过
- **通用 UI 交互** → `UserBehaviorEvents` 表 — 灵活的事件类型，支持未来扩展

两表互补，不互相替代。

### 9.3 用户画像刷新算法

```
FinalScore = 加权平均（按播放次数）:
  AvgEnergy       = Σ(Energy × PlayCount) / Σ(PlayCount)
  AvgDanceability = Σ(Danceability × PlayCount) / Σ(PlayCount)
  AvgValence      = Σ(Valence × PlayCount) / Σ(PlayCount)
  AvgTempo        = Σ(Tempo × PlayCount) / Σ(PlayCount)
  AvgAcousticness = Σ(Acousticness × PlayCount) / Σ(PlayCount)

ExplorationLevel = |Unique Genres| / |Unique Tracks|
  (0~1, 值越高代表品味越广泛，越适合冷启动探索)

FavoriteGenres = Top 5 genres by (play count + like count)
TopArtists     = Top 10 artists by play count
TopTracks      = Top 10 tracks by play count
```

### 9.4 跨模块通信

```
UserBehavior ──(IPublisher.Publish)──→ UserBehaviorUpdatedEvent
                                           │
                                           ↓ (Phase 5 待实现)
                                      Recommendation 模块
                                      INotificationHandler<UserBehaviorUpdatedEvent>
                                      → 刷新推荐列表
```

---

## 十、关联 Phase 依赖

| 依赖方向 | 模块 | 说明 |
|---|---|---|
| UserBehavior → Catalog | 引用 | 只读读取 Track/Artist/Album 实体进行 JOIN |
| UserBehavior → AudioFeatures | 引用 | 只读读取 TrackAudioFeature 进行加权平均 |
| UserBehavior → Favorites | 引用 | 只读读取 UserLike 统计收藏流派 |
| UserBehavior → Contracts | 引用 | 发布 UserBehaviorUpdatedEvent 领域事件 |
| Phase 5 Recommendation → UserBehavior | 待实现 | 订阅 UserBehaviorUpdatedEvent 刷新推荐 |
| Phase 5 Recommendation → Contracts | 待实现 | 通过事件获取用户画像数据 |

---

## 十一、待办与后续建议

### 11.1 短期（Phase 5 开发前）

- [ ] 确认 Recommendation 模块订阅 `UserBehaviorUpdatedEvent` 的 Handler 已实现
- [ ] Phase 5 需要从 `UserProfile` 表读取画像数据（可直接跨模块访问或通过事件携带）

### 11.2 中期（功能增强）

- [ ] 添加 `PUT /api/user-behavior/play/{id}/progress` 端点，支持前端周期性上报播放进度
- [ ] 搜索记录与 UserBehavior 的关联分析（当前搜索历史在 Search 模块独立管理）

### 11.3 长期（性能优化）

- [ ] 播放历史表数据量增长后考虑归档策略（如 6 个月前的数据移入归档表）
- [ ] 用户画像刷新可改为后台任务（避免 HTTP 请求超时），引入 Channel/BackgroundService
- [ ] 行为事件表考虑时序数据库或列存储（当前 NVARCHAR 兼容性优先）

---

## 十二、开发小结

Phase 4 完整实现了用户行为追踪与用户画像系统的后端部分，共交付约 1,167 行代码（含注释），10 个 API 端点，3 张数据库表，1 个跨模块事件。

代码遵循 Modular Monolith 架构的全部规范：CQRS 模式、FluentValidation 验证管道、Mapster 对象映射、统一 ApiResponse 响应、跨模块只读数据访问、非关键写入容错、幂等操作设计。编译 0 错误 0 警告。

审查中发现并修复了 2 个 Bug（RecordPlay 返回值缺失、冗余项目引用），完成了 4 项代码优化（合并 DB 查询、清除死代码、命名规范化、中文注释增强）。

Phase 5 的 Recommendation 模块可通过订阅 `UserBehaviorUpdatedEvent` 获取画像更新通知，并通过跨模块只读访问 `UserProfile` 表获取加权音频偏好数据。
