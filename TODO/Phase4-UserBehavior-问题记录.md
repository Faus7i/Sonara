# Phase 4 — UserBehavior 模块问题记录

> 审查日期：2026-05-22
> 审查范围：`src/Modules/UserBehavior/` 全部源码 + 关联文件
> 编译状态：0 错误 / 0 警告

---

## 问题 1：冗余项目引用 — MusicRec.Search

### 严重程度

🟢 低 — 不影响编译和运行，仅增加不必要的依赖关系

### 位置

- **文件**：`src/Modules/UserBehavior/MusicRec.UserBehavior.csproj`
- **行号**：L23

```xml
<!-- 当前 L22-24 -->
<ProjectReference Include="..\Catalog\MusicRec.Catalog.csproj" />
<ProjectReference Include="..\AudioFeatures\MusicRec.AudioFeatures.csproj" />
<ProjectReference Include="..\Favorites\MusicRec.Favorites.csproj" />
<ProjectReference Include="..\Search\MusicRec.Search.csproj" />       <!-- ← 冗余 -->
```

### 现象

所有 Handler 中未出现任何 `using MusicRec.Search.*` 引用，也未访问 Search 模块的任何实体（`UserSearchHistory` 等）。UserBehavior 模块不依赖 Search 模块即可完成所有功能。

### 影响

- **构建**：增加一次不必要的项目依赖解析，轻微拖慢增量编译
- **架构**：违反"模块最小依赖"原则，后续维护者可能误以为 UserBehavior 依赖 Search，产生错误理解
- **循环依赖风险**：若将来 Search 模块反向引用 UserBehavior，会形成循环引用错误

### 修改办法

删除 `MusicRec.UserBehavior.csproj` 第 24 行：

```xml
<ProjectReference Include="..\Search\MusicRec.Search.csproj" />
```

### 验证方式

1. 删除该行后执行 `dotnet build`，应 0 错误
2. 全量搜索 `src/Modules/UserBehavior/` 中 `MusicRec.Search` 字符串，确认无其他依赖

---

## 问题 2：RecordPlay 端点不返回播放历史 ID — 前后端协议断裂

### 严重程度

🟡 中 — 不影响编译，但导致前端无法直接调用后续的 FinishPlay / SkipPlay 端点

### 涉及文件

| 文件 | 角色 |
|---|---|
| `src/Modules/UserBehavior/Commands/UserBehaviorCommands.cs` (L9) | Command 定义，返回 `IRequest`（void） |
| `src/Modules/UserBehavior/Handlers/RecordPlayCommandHandler.cs` (L25-L52) | Handler 不返回任何值 |
| `src/Web/MusicRec.WebApi/Controllers/UserBehaviorController.cs` (L31-L37) | Controller 返回固定字符串，不含 ID |

### 现象

当前调用链：

```
POST /api/user-behavior/play { trackId, source }
  → Controller: RecordPlay(trackId, source)
    → Command: RecordPlayCommand (IRequest, 无返回值)
      → Handler: 创建 UserPlayHistory { Id = Guid.NewGuid(), ... }
      → SaveChangesAsync (非关键写入，失败静默)
  ← Controller 返回: { "success": true, "data": null, "message": "播放已记录" }
```

**但后续端点需要 `playHistoryId`**：

```
PUT  /api/user-behavior/play/{id}/finish   ← id = 播放历史记录 ID
PUT  /api/user-behavior/play/{id}/skip     ← id = 播放历史记录 ID
```

前端拿到 `RecordPlay` 的响应后，**没有途径获知刚创建的播放历史 ID**。

### 影响

- **前端开发阻塞**：必须额外调用 `GET /api/user-behavior/history` 并猜测哪条记录是最新创建的，存在竞态条件（多条播放记录并发时）
- **API 设计不完整**：RecordPlay → FinishPlay 形成多步骤操作链，但第一步不返回后续步骤所需的标识符
- **数据一致性问题**：前端"猜测"ID 可能导致 skip/finish 操作了错误的播放记录

### 修改办法

共需修改 3 处：

**① `Commands/UserBehaviorCommands.cs` L9 — 改为返回 Guid**

```csharp
// 修改前
public record RecordPlayCommand(Guid UserId, Guid TrackId, string? Source = null) : IRequest;

// 修改后
public record RecordPlayCommand(Guid UserId, Guid TrackId, string? Source = null) : IRequest<Guid>;
```

**② `Handlers/RecordPlayCommandHandler.cs` L14 — 接口改为 `IRequestHandler<RecordPlayCommand, Guid>`，返回 Id**

```csharp
// 修改前
public class RecordPlayCommandHandler : IRequestHandler<RecordPlayCommand>

// 修改后
public class RecordPlayCommandHandler : IRequestHandler<RecordPlayCommand, Guid>

// Handle 方法末尾改为
public async Task<Guid> Handle(RecordPlayCommand request, CancellationToken ct)
{
    // ... 现有逻辑 ...
    try { await _db.SaveChangesAsync(ct); }
    catch (Exception ex) { /* 现有日志 */ }
    
    return history.Id;  // ← 新增：无论写入成功或失败，都返回 ID
}
```

**③ `Controllers/UserBehaviorController.cs` L30-L37 — 接收返回值并包装到 ApiResponse**

```csharp
// 修改前
[HttpPost("play")]
public async Task<ActionResult<ApiResponse>> RecordPlay(Guid trackId, string? source = null)
{
    var userId = User.GetUserId();
    await _sender.Send(new RecordPlayCommand(userId, trackId, source));
    return Ok(ApiResponse.Ok("播放已记录"));
}

// 修改后
[HttpPost("play")]
[ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
public async Task<ActionResult<ApiResponse<Guid>>> RecordPlay(Guid trackId, string? source = null)
{
    var userId = User.GetUserId();
    var playHistoryId = await _sender.Send(new RecordPlayCommand(userId, trackId, source));
    return Ok(ApiResponse<Guid>.Ok(playHistoryId, "播放已记录"));
}
```

### 额外考虑

- Handler 中即使 `SaveChangesAsync` 失败（被 catch 静默），也返回 `history.Id`。前端拿到 ID 调用 finish/skip 时，Handler 会因为 `history is null` 而记录 Warning 日志后静默返回，不会抛异常。这是安全的行为。
- 如果不想让前端拿到"无效 ID"，可以将 `Guid.Empty` 作为失败信号，但这会增加前端判断复杂度。当前方案更简洁。

### 验证方式

1. `dotnet build` 0 错误
2. 启动后端，用 Swagger 调用 `POST /api/user-behavior/play`，确认响应 `data` 字段包含 GUID
3. 使用返回的 GUID 调用 `PUT /api/user-behavior/play/{id}/finish`，确认正常完成
