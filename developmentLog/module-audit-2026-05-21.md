# 全模块审查报告

> **日期**：2026-05-21  
> **范围**：7 个业务模块 + 2 个基础设施层（BuildingBlocks + Spotify）  
> **方法**：静态代码审查 + `dotnet build` 编译验证 + API 运行时启动检查  
> **结论**：构建 0 错误 0 警告，API 启动正常。发现 18 个问题（2 严重 / 3 高 / 5 中 / 4 低 / 4 信息），无安全漏洞。

---

## 一、测试结果总览

| 测试项 | 结果 |
|--------|------|
| `dotnet build` 全量编译 | ✅ 0 错误 0 警告 |
| API 启动 (`dotnet run`) | ✅ 进程存活，无崩溃 |
| 测试项目 | ⚠️ 项目无任何测试项目 |
| 数据库迁移 | ⚠️ 未运行（需 SQL Server 实例） |

---

## 二、问题清单

### 严重（运行时 NRE）

#### 🔴 问题 1：`ImportTrackCommandHandler` — `trackObj.Album` 为 null 导致 NRE

- **文件**：`src/Modules/Catalog/Handlers/ImportTrackCommandHandler.cs`
- **行号**：47, 61, 62
- **原因**：Spotify API 对本地文件/特殊曲目返回 `"album": null`。System.Text.Json 反序列化时将 `SimplifiedAlbumObject Album { get; set; } = new()` 覆盖为 `null`。Handler 未做 null 检查即传给 `GetOrCreateAlbumAsync()`，访问 `albumObj.Id` 触发 NRE。
- **对比**：`ImportArtistCommandHandler.cs` 第 61 行已有 `if (trackObj.Album is null) continue;` 防护，但 `ImportTrackCommandHandler` 未同步。
- **修复**：
  ```csharp
  // 在 ImportTrackCommandHandler 中，调用 GetOrCreateAlbumAsync 之前：
  if (trackObj.Album is null)
      continue;  // 跳过无专辑信息的本地曲目
  
  // 同样，访问 Album 嵌套属性前加 ?.
  var coverUrl = trackObj.Album?.Images?.FirstOrDefault()?.Url;
  ```

#### 🔴 问题 2：`ImportTrackCommandHandler` — `trackObj.Artists` 为 null 导致 NRE

- **文件**：`src/Modules/Catalog/Handlers/ImportTrackCommandHandler.cs`
- **行号**：48
- **原因**：同上。Spotify API 对特殊曲目返回 `"artists": null`，覆盖 `List<SimplifiedArtistObject> { get; set; } = new()` 为 null。传入 `GetOrCreateArtistsAsync` 后 `foreach (var obj in artistObjs)` 触发 NRE。
- **修复**：
  ```csharp
  var artists = await GetOrCreateArtistsAsync(trackObj.Artists ?? new(), ct);
  ```

---

### 高（潜在运行时 NRE）

#### 🟠 问题 3：`Images` 列表 null — `.FirstOrDefault()` NRE（4 处）

System.Text.Json 可能将 `"images": null` 覆盖 `List<ImageObject> Images { get; set; } = new()` 为 null。以下位置缺少 null 列表检查：

| 文件 | 行 | 代码 |
|------|-----|------|
| `Catalog/Handlers/ImportTrackCommandHandler.cs` | 62 | `trackObj.Album.Images.FirstOrDefault()?.Url` |
| `Catalog/Handlers/ImportTrackCommandHandler.cs` | 90 | `albumObj.Images.FirstOrDefault()?.Url` |
| `Catalog/Handlers/ImportArtistCommandHandler.cs` | 51 | `artistObj.Images.FirstOrDefault()?.Url` |
| `Catalog/Handlers/ImportArtistCommandHandler.cs` | 77, 93 | `trackObj.Album.Images.FirstOrDefault()?.Url` |

- **修复**：将 `.FirstOrDefault()?.Url` 改为 `?.FirstOrDefault()?.Url` 或 `(Images ?? []).FirstOrDefault()?.Url`
  ```csharp
  // Before
  albumObj.Images.FirstOrDefault()?.Url
  // After
  albumObj.Images?.FirstOrDefault()?.Url
  ```

#### 🟠 问题 4：`ImportArtistCommandHandler` — `artistObj.Genres.Count` NRE

- **文件**：`src/Modules/Catalog/Handlers/ImportArtistCommandHandler.cs`
- **行号**：50
- **代码**：`Genres = artistObj.Genres.Count > 0 ? string.Join(",", artistObj.Genres) : null`
- **原因**：Spotify 可能返回 `"genres": null`，覆盖 `List<string>` 为 null，`.Count` 触发 NRE
- **修复**：`artistObj.Genres is { Count: > 0 } ? string.Join(",", artistObj.Genres) : null`

#### 🟠 问题 5：`SearchMusicCommandHandler` — `a.Genres.Count` NRE

- **文件**：`src/Modules/Search/Handlers/SearchMusicCommandHandler.cs`
- **行号**：81（MapArtists 方法中）
- **代码**：`Genres: a.Genres.Count > 0 ? string.Join(",", a.Genres) : null`
- **修复**：同问题 4，`a.Genres is { Count: > 0 } ? ...`

---

### 中等（逻辑缺陷 / 并发安全）

#### 🟡 问题 6：4 个 Import Handler 缺少 `DbUpdateException` 并发处理

以下 Handler 在 `SaveChangesAsync` 时未 try-catch `DbUpdateException`（SQL 2601/2627），并发竞争时返回 500 而非 409：

| 文件 | SaveChanges 行 |
|------|:---:|
| `Catalog/Handlers/ImportTrackCommandHandler.cs` | 70 |
| `Catalog/Handlers/ImportArtistCommandHandler.cs` | 97 |
| `AudioFeatures/Handlers/ImportAudioFeaturesCommandHandler.cs` | 67 |
| `AudioFeatures/Handlers/GetAudioFeaturesQueryHandler.cs` | 44 |

**已有正确参考**：`Favorites/Handlers/LikeTrackCommandHandler.cs` 第 52-60 行，`Identity/Handlers/RegisterUserCommandHandler.cs` 第 56-64 行。

- **修复**（统一模板）：
  ```csharp
  try
  {
      await _db.SaveChangesAsync(ct);
  }
  catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
  {
      // 并发重复 → 重新查询返回已有数据
      var existing = await _db.Set<TEntity>()
          .Where(...).ToListAsync(ct);
      return existing.Adapt<IReadOnlyList<TDto>>();
  }
  ```

#### 🟡 问题 7：`ReorderPlaylistTracksCommandHandler` — 缺少 Count 验证

- **文件**：`src/Modules/Playlist/Handlers/ReorderPlaylistTracksCommandHandler.cs`
- **行号**：约 39-45
- **问题**：只验证了传入 `TrackIds` 都属于该歌单，但未验证 `request.TrackIds.Count == tracks.Count`。若客户端发送少于实际数的 ID 列表，未列出的曲目保留旧的 `OrderIndex` 值，产生间隙。
- **修复**：
  ```csharp
  if (request.TrackIds.Count != tracks.Count)
      throw new ValidationException("曲目 ID 数量与歌单曲目数不一致");
  ```

#### 🟡 问题 8：`ReorderPlaylistTracksCommandHandler` — 未检查重复 TrackId

- **文件**：同上
- **行号**：约 50
- **问题**：`request.TrackIds` 可能含重复项，`tracks.First(t => t.TrackId == request.TrackIds[i])` 会对同一 TrackId 分配不同 `OrderIndex`（最后一次写入获胜），导致不一致。
- **修复**：
  ```csharp
  if (request.TrackIds.Distinct().Count() != request.TrackIds.Count)
      throw new ValidationException("曲目 ID 列表包含重复项");
  ```

#### 🟡 问题 9：`EntityConfigurationRegistry` — `List<Assembly>` 线程不安全

- **文件**：`src/BuildingBlocks/Infrastructure/EntityConfigurationRegistry.cs`
- **行号**：16
- **问题**：`private static readonly List<Assembly> _assemblies = new()` 在 `Register()`（写）和 `ApplyConfigurations()`（读）之间无同步。当前在单线程 DI 构建阶段调用的假设是脆弱的。
- **修复**：改用 `ConcurrentBag<Assembly>` 或加 `lock`

#### 🟡 问题 10：Favorites/Playlist 跨模块直接引用 `Catalog.Entities`

| 文件 | 引用 |
|------|------|
| `Playlist/Handlers/AddTrackToPlaylistCommandHandler.cs` | `using MusicRec.Catalog.Entities;` |
| `Playlist/Handlers/GetPlaylistDetailQueryHandler.cs` | `using MusicRec.Catalog.Entities;` |
| `Favorites/Handlers/LikeTrackCommandHandler.cs` | `using MusicRec.Catalog.Entities;` |
| `Favorites/Handlers/GetUserLikesQueryHandler.cs` | `using MusicRec.Catalog.Entities;` |

架构规则（规则 17）允许跨模块只读 `_db.Set<T>()`，但直接 `using` 实体命名空间使两个模块在编译时耦合。若 Catalog 实体变更，Favorites/Playlist 编译可能失败。

- **方案**：当前可接受（规则允许），但长期建议将共享实体提取到 `MusicRec.Contracts` 或共享 abstractions 中。

---

### 低（代码质量 / 注释）

#### 🔵 问题 11：`LoginUserCommandHandler` — `||` 与注释不一致

- **文件**：`src/Modules/Identity/Handlers/LoginUserCommandHandler.cs`
- **行号**：43
- **问题**：注释写"使用 `|` 而非 `||`"，实际代码用了 `||`。无安全影响（PBKDF2 计算始终执行），但代码与文档不一致。
- **修复**：将 `||` 改为 `|`，或修正注释。

#### 🔵 问题 12：`IdentityController.GetUserId()` — 使用 `Guid.Parse` 而非扩展方法

- **文件**：`src/Web/MusicRec.WebApi/Controllers/IdentityController.cs`
- **行号**：86-89
- **问题**：使用 `Guid.Parse(sub)`，格式异常时抛 `FormatException` → 500（非 `UnauthorizedException`）。已有 `ClaimsPrincipalExtensions.GetUserId()`（`Guid.TryParse` + `UnauthorizedException`）。
- **修复**：统一使用 `User.GetUserId()` 扩展方法，删除私有 `GetUserId()`。

#### 🔵 问题 13：`SearchMusicCommandHandler` — 空的 `catch {}` 无声

- **文件**：`src/Modules/Search/Handlers/SearchMusicCommandHandler.cs`
- **行号**：44-47
- **问题**：搜索历史写入失败被空 `catch {}` 静默吞掉。设计意图正确（不阻塞搜索），但生产环境排查困难。
- **修复**：至少注入 `ILogger` 记录 Warning：
  ```csharp
  catch (Exception ex)
  {
      _logger.LogWarning(ex, "搜索历史写入失败（已忽略）");
  }
  ```

#### 🔵 问题 14：`PasswordHasher` — OWASP 迭代次数注释不准确

- **文件**：`src/BuildingBlocks/Shared/PasswordHasher.cs`
- **行号**：16, 18
- **问题**：注释写"OWASP 2023 推荐最少 100,000 次"，但 OWASP 2023 实际推荐 **600,000** 次（PBKDF2-SHA256）。`100_000` 对低风险场景可接受，但引用的标准有误。
- **修复**：更正注释为"OWASP 2018 推荐最少 100,000 次"，或将迭代次数提升到 `600_000`。

---

### 信息（可优化）

#### ℹ️ 问题 15：`SpotifyTokenStore` — 缓存击穿（惊群效应）

- **文件**：`src/Infrastructure/Spotify/SpotifyTokenStore.cs`
- **行号**：39-43
- **问题**：`TryGetValue` + `FetchTokenAsync` + `Set` 非原子操作。缓存过期瞬间多个并发请求同时获取 token。
- **建议**：使用 `SemaphoreSlim` 或 `Lazy<Task<string>>` 确保单次获取。
- **影响**：仅浪费少量 API 调用，不影响正确性。

#### ℹ️ 问题 16：`SpotifyAuthHandler` — 缺少 null token 防御

- **文件**：`src/Infrastructure/Spotify/SpotifyAuthHandler.cs`
- **行号**：26-27
- **问题**：若 `GetAccessTokenAsync()` 返回 null/empty，Authorization 头变成 `Bearer null`。
- **建议**：加 `if (string.IsNullOrEmpty(token)) throw ...`

#### ℹ️ 问题 17：`GetAlbumQueryHandler` — 手写 DTO 构造

- **文件**：`src/Modules/Catalog/Handlers/GetAlbumQueryHandler.cs`
- **行号**：41-51
- **问题**：使用手写 `new AlbumDto(...)` 而非 `album.Adapt<AlbumDto>()`。注释称性能优化，但与项目 Mapster 模式不一致。
- **建议**：无强制修改必要，但字段变更时需记住同步维护。

#### ℹ️ 问题 18：无测试项目

- 项目没有任何测试项目（未找到 `*.Tests.csproj`），建议为基础模块（Identity、Catalog、Favorites）增加单元测试。

---

## 三、已验证通过的模块

`dotnet build` 0 错误 0 警告；API 启动正常。以下模块在架构合规性、安全性和正确性方面验证通过：

| 模块 | 关键检查 | 结果 |
|------|---------|:---:|
| **Identity** | CQRS、FluentValidation 全覆盖、密码安全 (PBKDF2 100K)、JWT 密钥长度验证、DomainException 异常处理、无 SQL 注入 | ✅ |
| **Catalog** | Cache-Aside 模式、Mapster 映射、验证器覆盖 | ✅（另有 null 覆盖问题） |
| **AudioFeatures** | Cache-Aside 模式、Mapster 映射、ToVector() 正确 | ✅（另有并发问题） |
| **Search** | Spotify `?.` + `??` 空值防护（大部分）、非关键写入不阻塞 | ✅（另有 Genres null 问题） |
| **Favorites** | 幂等写操作、DbUpdateException 并发处理、所有权验证、跨模块只读 `_db.Set<T>()` | ✅ 完美 |
| **Playlist** | 所有权验证（5/5 操作）、Cascade 删除、幂等移除曲目 | ✅（另有 Reorder 问题） |
| **Player** | 零实体模式、纯 ISpotifyClient 代理、`?.` 空值防护完整 | ✅ 完美 |
| **BuildingBlocks** | 单 DbContext、EntityConfigurationRegistry、GlobalExceptionMiddleware（DomainException 全覆盖）、ValidationBehavior 单次全局注册、ApiResponse 统一格式 | ✅ |
| **Spotify** | DelegatingHandler 线程安全、Token 缓存 3500s、Polly 5 次指数退避 + 抖动、`SpotifyJsonDefaults` 单例 | ✅ |

---

## 四、修复优先级建议

| 优先级 | 问题编号 | 修复时间 |
|--------|:---:|:---:|
| P0 — 立即 | 1, 2 (Album/Artists NRE) | ~10 分钟 |
| P1 — 本周 | 3, 4, 5 (Images/Genres NRE) | ~15 分钟 |
| P1 — 本周 | 6 (DbUpdateException 并发) | ~20 分钟 |
| P2 — 本月 | 7, 8 (Reorder 验证) | ~15 分钟 |
| P2 — 本月 | 12 (Guid.Parse → TryParse) | ~5 分钟 |
| P3 — 后续 | 9, 10, 11, 13, 14 | ~30 分钟 |
| P4 — 可选 | 15, 16, 17, 18 | 按需 |
