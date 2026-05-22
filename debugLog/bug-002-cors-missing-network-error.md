# Bug 002: 前端请求后端 API 报 Network Error — CORS 未配置

## 基本信息

| 项目 | 内容 |
|------|------|
| **严重等级** | P0 — 核心功能完全不可用 |
| **发现时间** | 2026-05-22 12:00 |
| **修复时间** | 2026-05-22 12:15 |
| **影响模块** | 后端 — Program.cs / 全局中间件管道 |
| **影响范围** | 前端所有 API 请求（注册、登录、推荐、收藏等全部端点） |

## 问题描述

用户在浏览器中访问前端 (http://localhost:3000)，执行注册/登录操作时，浏览器控制台报 **"Network Error"**，请求无法到达后端。

## 具体表现

1. 注册页面填写完整信息后点击"注册" → 前端显示 "Network Error"
2. 登录页面同样无法工作
3. 浏览器 DevTools Network 面板显示请求被拦截（CORS 阻止）
4. 直接通过 `curl` 调用 `localhost:5000/api/identity/register` 正常返回（后端功能正常）

## 触发条件

- 任何通过浏览器前端 (origin: `http://localhost:3000`) 发起的 API 请求
- 后端 (origin: `http://localhost:5000`) 与前端不同源（不同端口即跨域）

## 根因分析

`Program.cs` 中**完全没有 CORS 配置**：

1. 未调用 `builder.Services.AddCors(...)` — 未注册 CORS 服务
2. 未调用 `app.UseCors(...)` — 未启用 CORS 中间件

浏览器的同源策略阻止了所有来自 `localhost:3000` 到 `localhost:5000` 的跨域请求。服务器未返回 `Access-Control-Allow-Origin` 响应头，浏览器直接拦截，表现为 "Network Error"。

虽然通过 `curl` 测试后端 API 正常，但 `curl` 不执行 CORS 检查，所以没有暴露此问题。

## 修复策略

在 `Program.cs` 中添加 CORS 配置：

### 1. 注册 CORS 服务（在 `builder.Services` 段）

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:FrontendOrigin"] ?? "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

关键设计决策：
- `WithOrigins()` 而非 `AllowAnyOrigin()` — 因为 JWT 认证需要 `AllowCredentials()`，两者不兼容
- `AllowCredentials()` — 允许携带 Authorization 头
- 前端地址通过配置 `Cors:FrontendOrigin` 可覆盖，默认 `http://localhost:3000`

### 2. 启用 CORS 中间件（在 `app` 管道段）

CORS 中间件必须在 `UseResponseCompression` 之后、`UseAuthentication` 之前：

```csharp
app.UseResponseCompression();          // 3. 响应压缩
app.UseCors("AllowFrontend");           // 4. 跨域请求
```

## 修改文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `src/Web/MusicRec.WebApi/Program.cs` | 修改 | 新增 CORS 服务注册 + 中间件启用 |

## 修复前后对比

### 修复前
```
// 无 CORS 相关代码
builder.Services.AddResponseCompression(...);  // 直接进入压缩配置
...
app.UseResponseCompression();   // 中间件管道无 CORS
app.UseAuthentication();
```

**结果**: 浏览器拦截所有跨域请求 → Network Error

### 修复后
```
// CORS 服务注册
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

// 中间件管道（CORS 在 Auth 之前）
app.UseCors("AllowFrontend");    // ← 新增
app.UseAuthentication();
```

**结果**: 预检请求返回正确 CORS 头，实际请求正常通过

## 验证结果

### CORS 预检 (OPTIONS)
```
HTTP/1.1 204 No Content
Access-Control-Allow-Credentials: true
Access-Control-Allow-Methods: POST
Access-Control-Allow-Origin: http://localhost:3000
```

### 实际注册请求 (POST)
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJ...",
    "user": { "id": "...", "email": "newtest@test.com", "nickname": "TestUser" }
  },
  "message": "注册成功"
}
```

### 编译
```
dotnet build — 0 警告, 0 错误
```
