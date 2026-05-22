# Bug 005b: 前端搜索持续报 Network Error — 追加修复

## 基本信息

| 项目 | 内容 |
|------|------|
| **严重等级** | P0 — 搜索功能完全不可用 |
| **发现时间** | 2026-05-22 14:00 |
| **修复时间** | 2026-05-22 14:15 |
| **关联 Bug** | Bug 005（前后端类型不匹配） |
| **影响范围** | 前端所有 API 请求 |

## 问题描述

Bug 005 修复了类型不匹配后，前端仍然报 **"网络错误，请确保后端服务正在运行"**。但 `curl` 直接访问后端 API 正常返回数据，CORS 头也正确配置。

## 根因分析

根本原因是 **浏览器端跨域请求失败** 与 **CORS 中间件配置完全正确**之间的矛盾。

排查过程：
1. ✅ 后端 API 正常 → `curl localhost:5000/api/search` 返回数据
2. ✅ CORS 响应头正确 → `Access-Control-Allow-Origin: http://localhost:3000`
3. ✅ CORS 预检通过 → OPTIONS 返回 204
4. ✅ 后端同时监听 IPv4 (`127.0.0.1`) 和 IPv6 (`::1`)
5. ❌ 浏览器端请求仍然失败

可能原因（无法在服务端复现）：
- Windows 防火墙/Defender 拦截浏览器对 5000 端口的出站连接
- DNS 解析差异（浏览器 vs curl 的 localhost 解析路径不同）
- Next.js Turbopack 开发服务器与浏览器的 Websocket/HMR 干扰

## 修复策略

**根本解决方案**：引入 Next.js API 代理层，彻底消除跨域。

### 架构变更

```
修复前（浏览器直连后端 — 跨域）：
  浏览器 (localhost:3000) ──跨域──▶ 后端 (localhost:5000)

修复后（Next.js 代理 — 同源）：
  浏览器 (localhost:3000) ──同源──▶ Next.js API Route ──服务端──▶ 后端 (localhost:5000)
```

### 实现

1. **新建 `frontend/src/app/api/[...path]/route.ts`** — Next.js 全量 API 代理
   - 捕获所有 `/api/*` 请求
   - 转发到 `http://127.0.0.1:5000`（服务端到服务端，无跨域）
   - 支持全部 HTTP 方法（GET/POST/PUT/DELETE/PATCH）
   - 转发原始请求头和 Body
   - 502 降级响应（后端不可用时）

2. **修改 `frontend/src/lib/api-client.ts`** — baseURL 改为相对路径
   ```diff
   - const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';
   + const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || '/api';
   ```

3. **新建 `frontend/.env.local`** — 明确环境变量
   ```
   NEXT_PUBLIC_API_URL=/api
   ```

## 修改文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `frontend/src/app/api/[...path]/route.ts` | 新建 | Next.js 全量 API 代理路由 |
| `frontend/src/lib/api-client.ts` | 修改 | baseURL 改为相对路径 `/api` |
| `frontend/.env.local` | 新建 | 环境变量配置 |

## 优势

1. **零跨域** — 浏览器只与同源 Next.js 通信
2. **无需 CORS** — 服务端间通信不需要 CORS 头
3. **安全性** — 后端无需暴露 CORS 策略
4. **生产就绪** — 生产环境可用 Nginx 替代 Next.js 代理，无需改代码
5. **统一入口** — 所有 API 请求经过单一代理，便于添加日志、限流等中间件

## 验证结果

| 检查项 | 结果 |
|--------|------|
| `curl localhost:3000/api/search?q=avicii`（通过代理） | ✅ 返回 3 首 Avicii 曲目 |
| `curl localhost:3000/search`（页面） | ✅ HTTP 200 |
| `npx tsc --noEmit` | ✅ 0 错误 |
