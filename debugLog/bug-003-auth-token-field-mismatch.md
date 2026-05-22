# Bug 003: 登录/注册后 Token 丢失 — 前后端字段名不匹配

## 基本信息

| 项目 | 内容 |
|------|------|
| **严重等级** | P0 — 核心认证功能不可用 |
| **发现时间** | 2026-05-22 12:25 |
| **修复时间** | 2026-05-22 12:30 |
| **影响模块** | 前端 — auth-store.ts / types/api.ts |
| **影响范围** | 登录、注册流程完全瘫痪，用户无法保持登录状态 |

## 问题描述

用户注册成功后输入账号密码点击登录，页面看似登录成功（短暫跳转到首页），但随即被踢回登录页面。直接通过 `curl` 调用后端 API 正常返回 Token。

## 具体表现

1. 注册/登录后短暂跳转首页 → 瞬间强制跳回 `/login`
2. localStorage 中 `auth-token` 值为字符串 `"undefined"`（而非 JWT）
3. 前端控制台无直观错误（401 拦截器静默跳转）
4. 后端日志显示登录请求 200 成功返回

## 触发条件

- 任何用户通过前端界面执行注册或登录操作

## 根因分析

**前后端字段名不匹配**：

| 层 | 字段名 |
|----|--------|
| 后端 `AuthResultDto` | `AccessToken` |
| JSON 序列化 (CamelCase) | `accessToken` |
| 前端 `AuthResponse` 类型 | ~~`token`~~ |
| `auth-store.ts` 读取 | ~~`result.token`~~ |

**完整调用链路**：
```
1. POST /api/identity/login → 200 { success: true, data: { accessToken: "eyJ...", user: {...} } }
2. API Client 响应拦截器 → 解包 body.data → { accessToken, expiresAt, user }
3. authApi.login() → 返回 { accessToken, expiresAt, user }
4. auth-store.login() → const result = await authApi.login(data)
5. result.token → undefined  ❌  (实际字段是 accessToken)
6. localStorage.setItem('auth-token', undefined) → 存储字符串 "undefined"
7. set({ token: undefined, ... }) → Zustand 中 token 为 undefined
8. router.push('/') → 跳转首页
9. 首页 useQuery(['recommendations']) → API Client 从 localStorage 读取 token
10. Authorization: Bearer undefined → 后端返回 401
11. 401 响应拦截器 → 清空 localStorage → window.location.href = '/login'
12. 用户回到登录页面
```

**为什么 `curl` 测试正常**：`curl` 不经过前端认证流程，直接返回 JSON 观察正常。

**为什么注册"看似成功"**：注册接口也返回 `accessToken`，同样字段不匹配。但注册后 Zustand 中 `isAuthenticated: true`（因为 `result.user` 正常），导致首页短暂渲染后才触发 401 循环。

## 修复策略

### 1. 修正前端类型定义 (`types/api.ts`)

```diff
- export interface AuthResponse {
-   token: string;
-   user: User;
- }
+ export interface AuthResponse {
+   accessToken: string;
+   expiresAt: string;
+   user: User;
+ }
```

`expiresAt` 虽然没有被直接使用，但加上它使得类型与后端 DTO 完整对齐，方便未来做 Token 过期前自动刷新。

### 2. 修正 Store 中的字段访问 (`auth-store.ts`)

```diff
- localStorage.setItem('auth-token', result.token);
+ localStorage.setItem('auth-token', result.accessToken);
- set({ user: result.user, token: result.token, isAuthenticated: true });
+ set({ user: result.user, token: result.accessToken, isAuthenticated: true });
```

## 修改文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `frontend/src/types/api.ts` | 修改 | `AuthResponse.token` → `accessToken`，新增 `expiresAt` |
| `frontend/src/store/auth-store.ts` | 修改 | 2 处 `result.token` → `result.accessToken` |

## 修复前后对比

### 修复前
```ts
// types/api.ts
interface AuthResponse {
  token: string;     // ← 与后端 accessToken 不匹配
  user: User;
}

// auth-store.ts
const result = await authApi.login(data);
localStorage.setItem('auth-token', result.token);  // undefined
set({ user: result.user, token: result.token });     // undefined
```

**结果**: Token 丢失 → 401 → 强制跳回登录页

### 修复后
```ts
// types/api.ts
interface AuthResponse {
  accessToken: string;  // ← 与后端对齐
  expiresAt: string;    // ← 新增，完整对齐后端
  user: User;
}

// auth-store.ts
const result = await authApi.login(data);
localStorage.setItem('auth-token', result.accessToken);  // "eyJ..."
set({ user: result.user, token: result.accessToken });    // "eyJ..."
```

**结果**: Token 正确存取 → 后续请求携带有效 Bearer Token → 登录状态保持

## 验证结果

| 检查项 | 结果 |
|--------|------|
| TypeScript 类型检查 (`npx tsc --noEmit`) | 0 错误 |
| 后端登录 API (`curl` 直接调用) | 200 + 有效 JWT |
| 字段对齐 | `accessToken` ↔ `accessToken` ↔ `accessToken` |
