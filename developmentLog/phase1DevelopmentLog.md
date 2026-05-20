# Phase 1 开发日志：项目初始化 + 用户系统

> **日期**: 2026-05-20 ~ 2026-05-21  
> **分支**: main  
> **SDK**: .NET 9.0.314  
> **数据库**: SQL Server `.\SQLEXPRESS` → `MusicRecDb`

---

## 1. 开发目标

按照《基于.NET平台的个性化音乐推荐系统的开发文档》第 17 节 Phase 1 要求，完成以下四项：

| # | 目标 | 说明 |
|---|------|------|
| 1 | 项目初始化 | 创建 Modular Monolith 目录结构、7 个 .csproj、解决方案文件 |
| 2 | global.json | 锁定 .NET 9 SDK 版本（9.0.314），rollForward=disable |
| 3 | SQL Server | 连接 `.\SQLEXPRESS`，创建单数据库 `MusicRecDb`，EF Core 自动建表 |
| 4 | JWT + 用户系统 | 注册/登录/JWT 签发/资料管理，完整 CQRS 模式 |

---

## 2. 交付内容

### 2.1 项目结构

```
D:\Sonara\
├── MusicRec.sln                          # 解决方案（7 个项目）
├── global.json                           # .NET 9.0.314 锁定
│
├── src/
│   ├── BuildingBlocks/
│   │   ├── Shared/                       # MusicRec.Shared
│   │   │   ├── ApiResponse.cs            # 统一 {success,data,message,errors}
│   │   │   ├── Exceptions.cs             # 5 种自定义异常
│   │   │   ├── ValidationBehavior.cs     # MediatR 自动验证管道
│   │   │   └── PasswordHasher.cs         # PBKDF2-SHA256 密码哈希
│   │   │
│   │   ├── Abstractions/                 # MusicRec.Abstractions
│   │   │   ├── IEntity.cs               # 实体基础接口
│   │   │   └── IPasswordHasher.cs        # 密码哈希服务接口
│   │   │
│   │   ├── Contracts/                    # MusicRec.Contracts
│   │   │   └── Events/
│   │   │       └── UserRegisteredEvent.cs # 跨模块事件
│   │   │
│   │   └── Infrastructure/               # MusicRec.Infrastructure
│   │       ├── MusicRecDbContext.cs       # 共享 DbContext
│   │       ├── EntityConfigurationRegistry.cs
│   │       └── Migrations/               # EF Core 迁移
│   │
│   ├── Modules/
│   │   └── Identity/                     # MusicRec.Identity（用户系统）
│   │       ├── Entities/User.cs
│   │       ├── DTOs/                     # 4 个 DTO/Request
│   │       ├── Commands/                 # 3 个 Command
│   │       ├── Queries/                  # 1 个 Query
│   │       ├── Handlers/                 # 4 个 Handler
│   │       ├── Validators/               # 3 个 Validator
│   │       ├── Configurations/           # EF 表映射
│   │       ├── Services/JwtService.cs
│   │       └── DependencyInjection.cs
│   │
│   ├── Infrastructure/
│   │   └── Spotify/                      # MusicRec.Spotify（Phase 2 填充）
│   │
│   └── Web/
│       └── MusicRec.WebApi/              # MusicRec.WebApi
│           ├── Program.cs                # 启动配置
│           ├── appsettings.json
│           ├── DesignTimeDbContextFactory.cs
│           ├── Controllers/
│           │   └── IdentityController.cs  # 4 个 API 端点
│           ├── Middlewares/
│           │   └── GlobalExceptionMiddleware.cs
│           └── Infrastructure/
│               └── JsonDefaults.cs
```

### 2.2 数据库

| 表 | 字段 | 说明 |
|---|---|---|
| `Users` | Id, Email(unique), PasswordHash, Nickname, AvatarUrl, CreatedAt | 用户表 |
| `__EFMigrationsHistory` | — | EF Core 迁移历史 |

### 2.3 API 端点

| 方法 | 路径 | 认证 | 功能 |
|------|------|------|------|
| POST | `/api/identity/register` | 无 | 注册（返回 JWT） |
| POST | `/api/identity/login` | 无 | 登录（返回 JWT） |
| GET | `/api/identity/profile` | JWT | 获取当前用户资料 |
| PUT | `/api/identity/profile` | JWT | 更新昵称/头像 |

### 2.4 技术栈版本

| 包 | 版本 |
|---|---|
| .NET SDK | 9.0.314 |
| EF Core | 9.0.5 |
| MediatR | 12.5.0 |
| FluentValidation | 11.11.0 |
| Mapster | 7.4.0 |
| Serilog.AspNetCore | 9.0.0 |
| JWT Bearer | 9.0.5 |
| Swashbuckle | 6.9.0 |

---

## 3. 测试结果

### 3.1 初次测试（2026-05-20）

| # | 场景 | HTTP | 结果 |
|---|------|------|------|
| 1 | 注册新用户 | 200 | ✅ JWT + 用户资料 |
| 2 | 重复注册（冲突） | 409 | ✅ `success:false`，"该邮箱已被注册" |
| 3 | 弱密码 < 6 位 | 400 | ✅ FluentValidation 拦截 |
| 4 | 无效邮箱格式 | 400 | ✅ FluentValidation 拦截 |
| 5 | 正确密码登录 | 200 | ✅ JWT + 用户资料 |
| 6 | 错误密码登录 | 401 | ✅ `success:false`，"邮箱或密码错误" |
| 7 | 获取用户资料 | 200 | ✅ 完整 UserProfileDto |
| 8 | 更新资料 | 200 | ✅ 昵称 + 头像 URL 已更新 |
| 9 | 空昵称更新 | 400 | ✅ `success:false`，"昵称不能为空" |
| 10 | 无 Token 访问 | 401 | ✅ `{success,message,errors}` 格式 |
| 11 | 伪造 Token | 401 | ✅ `{success,message,errors}` 格式 |

### 3.2 优化后复测（2026-05-21）

全部 11 项测试与初次结果一致，无回归。

---

## 4. 审计与修复记录

### 4.1 第一次审计（项目结构与配置）

**审计范围**: 所有 .csproj、global.json、MusicRec.sln、目录结构、项目引用关系  
**审计结果**: 全部通过，零违规。

| 检查项 | 状态 |
|---|---|
| 7 个项目全部使用 `net9.0` | ✅ |
| 无预览包/RC 包 | ✅ |
| EF Core 包均为 9.0.5 | ✅ |
| global.json SDK 锁定且 rollForward=disable | ✅ |
| 7 个项目均在解决方案中 | ✅ |
| 目录结构匹配开发文档 | ✅ |
| 无跨模块引用违规 | ✅ |

### 4.2 第二次审计（代码架构）

**审计范围**: 全部 .cs 源文件  
**发现 6 个问题**，已全部修复。

| # | 严重性 | 问题 | 根因 | 修复方案 |
|---|--------|------|------|----------|
| 1 | **高** | 缺少 `UpdateProfileCommandValidator` | 初次开发遗漏 | 新建验证器，校验 Nickname 非空+长度、AvatarUrl 长度 |
| 2 | **高** | `JwtService` 对配置值使用 `!` 空值合并，配置缺失时静默 NRE | 未做启动期配置校验 | 构造函数从 DI 容器读取并验证配置，缺失立即抛出 `InvalidOperationException`；`Program.cs` 启动时同样验证 |
| 3 | **中** | `GetUserId()` 在 `sub` 为 null 时抛 NRE | 未防御 JWT 异常情况 | 添加 `if (sub is null) throw new UnauthorizedException(...)` |
| 4 | **中** | `ValidationBehavior` 中 `FluentValidation.ValidationException` 与自定义 `MusicRec.Shared.ValidationException` 命名空间冲突 | 两个同名类型存在于不同命名空间 | 使用完全限定名 `MusicRec.Shared.ValidationException` |
| 5 | **中** | `Program.cs` 未调用 `Log.CloseAndFlush()` | Serilog 标准模式遗漏 | 用 `try { app.RunAsync() } finally { Log.CloseAndFlush() }` 包裹 |
| 6 | **低** | EF 迁移自动应用无 try-catch | 数据库不可用时应用崩溃 | try-catch 包装 + 日志记录，迁移失败不阻止启动 |

---

## 5. 代码优化记录

### 5.1 消除重复：提取 IPasswordHasher 服务

**问题**: `RegisterUserCommandHandler` 和 `LoginUserCommandHandler` 各自内联了 `HashPassword()` 和 `VerifyPassword()` 方法（约 15 行重复代码），违反 DRY 原则。

**方案**:
- 新建 `MusicRec.Abstractions.IPasswordHasher` 接口（`Hash()` / `Verify()`）
- 新建 `MusicRec.Shared.PasswordHasher` 实现（PBKDF2-SHA256，100K 迭代）
- 两个 Handler 改为构造函数注入 `IPasswordHasher`
- 魔法数字 `16`、`32`、`100_000` 提取为命名常量 `SaltSize`、`HashSize`、`Iterations`

**影响**: `MusicRec.Shared.csproj` 新增对 `MusicRec.Abstractions` 的引用。

### 5.2 不可变性：IReadOnlyList 替代 List

**问题**: `ApiResponse<T>.Errors` 和 `ApiResponse.Errors` 为 `List<string>`，外部代码（中间件）可意外修改已构造的响应对象。

**方案**: 改为 `IReadOnlyList<string>`，默认值从 `new List<string>()` 改为 `Array.Empty<string>()`。`ValidationException.ValidationErrors` 同步修改以保持类型一致。

### 5.3 消除重复：共享 JsonSerializerOptions

**问题**: `GlobalExceptionMiddleware` 和 `Program.cs` JWT 事件中各自 `new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }`，重复创建相同配置对象。

**方案**: 新建 `MusicRec.WebApi.Infrastructure.JsonDefaults` 静态类，暴露 `CamelCaseOptions` 单例，两处引用。

### 5.4 清理：移除未使用的 using 和无用代码

- `ValidationBehavior.cs`: 移除 `using FluentValidation.Results;`（未使用）、移除 `.Where(f => f != null)` 过滤（`ErrorMessage` 永不为 null）
- `Program.cs`: 移除 `using System.Text.Json.Serialization;`（仅在 `.AddJsonOptions()` lambda 内使用，改为完全限定写法）

### 5.5 增强：Swagger 响应文档

`IdentityController` 全部 4 个端点添加 `[ProducesResponseType]` 注解，Swagger UI 可准确展示 200/400/401/409 各状态码的响应 schema。

### 5.6 注释：从 WHAT 到 WHY

所有源文件的注释从描述"是什么"（类名即自解释）改为解释"为什么"——设计决策、算法选择、安全考量：

| 文件 | 关键 WHY 注释 |
|------|--------------|
| `PasswordHasher.cs` | OWASP 2023 推荐 PBKDF2-SHA256 ≥100K 迭代；恒定时间比较防时序攻击 |
| `LoginUserCommandHandler.cs` | 无论用户是否存在都执行哈希验证，防响应时间枚举邮箱 |
| `EntityConfigurationRegistry.cs` | 说明所有 Register 调用在 DI 构建阶段（单线程），无需同步 |
| `UserConfiguration.cs` | Email 唯一索引作为数据库层并发防线 |
| `MusicRecDbContext.cs` | 应用层分配 Guid 确保领域事件发布前 ID 已确定 |
| `Program.cs` | 中间件管道顺序说明（Serilog→异常→Auth→Controller） |

---

## 6. 架构决策记录

### ADR-001: 使用 EntityConfigurationRegistry 解耦模块与 DbContext

**问题**: 各模块的 `IEntityTypeConfiguration<T>` 实现在模块程序集中，而 `MusicRecDbContext` 在 `BuildingBlocks/Infrastructure` 中，后者不能引用模块程序集。

**方案**: 创建静态注册表 `EntityConfigurationRegistry`。模块的 DI 扩展方法在启动时 `Register` 自己的程序集；DbContext 的 `OnModelCreating` 通过 `ApplyConfigurations` 遍历已注册程序集。

**替代方案**: 将实体配置放在 Infrastructure 程序集中。缺点：配置与实体分离，维护不便。

### ADR-002: JWT Secret 明文存储

**当前状态**: `appsettings.json` 中明文存储 JWT Secret。  
**风险等级**: 中（开发阶段可接受）  
**计划**: 上线前迁移到 `dotnet user-secrets` 或环境变量。

---

## 7. 已知限制

| # | 限制 | 影响 | 计划 |
|---|------|------|------|
| 1 | 无 API 版本控制 | 未来接口变更可能破坏客户端 | Phase 3 前加入 |
| 2 | 无 `UpdatedAt` 字段 | 无法追溯资料修改时间 | 需要时新增 |
| 3 | JWT 无刷新机制 | Token 过期需重新登录 | Phase 3 评估 |
| 4 | 无速率限制 | 可被暴力枚举 | 上线前加入 |
| 5 | Spotify 模块为空壳 | Phase 2 填充 | 下一步 |

---

## 8. Phase 2 准备

以下基础设施已就位，可直接开始 Phase 2：

- `MusicRec.Spotify` 项目（已创建，引用 Shared）
- `MusicRecDbContext` 支持多模块注册实体配置
- `MediatR` 管道 + `FluentValidation` 自动验证
- 全局异常处理中间件
- Serilog 请求日志 + 文件日志
- JWT 认证 + 统一 401/403 响应
- Swagger 文档

### Phase 2 目标

- Spotify Web API 适配器（OAuth、搜索、元数据、Audio Features）
- Catalog 模块（歌曲/专辑/歌手数据落库）
- AudioFeatures 模块（音频特征向量化）
- 前端项目初始化（Next.js + TypeScript）
