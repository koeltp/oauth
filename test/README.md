# OAuth 第三方测试客户端

本目录包含三个测试项目，用于模拟第三方应用通过本 OAuth 服务进行登录和授权的完整流程。

## 运行方式

### 前置条件

确保 OAuth 服务端已启动，测试客户端已在服务端注册并审核通过。

### 一键启动所有测试项目

```bash
# 从仓库根目录执行

# Web 测试应用（Authorization Code + PKCE）
dotnet run --project test/TestClient.Web

# 控制台测试套件（Client Credentials）
dotnet run --project test/TestClient.Console

# SPA 测试应用（Public Client + PKCE）
node test/TestClient.Spa/server.js
```

### 项目概览

| 项目 | 运行命令 | 端口 | 测试内容 |
|---|---|---|---|
| TestClient.Web | `dotnet run --project test/TestClient.Web` | 5002 | Authorization Code + PKCE 完整流程 |
| TestClient.Console | `dotnet run --project test/TestClient.Console` | 命令行 | Client Credentials 客户端凭证流程 |
| TestClient.Spa | `node test/TestClient.Spa/server.js` | 5003 | Public Client + PKCE 纯前端流程 |

## 项目结构

```
test/
├── TestClient.Web/         # Web 测试应用 — 模拟有后端的 Web 应用
│   ├── Program.cs          # 完整测试路由
│   └── appsettings.json    # OAuth 服务器配置
├── TestClient.Console/     # 控制台测试套件 — 模拟服务端应用
│   └── Program.cs          # 菜单驱动测试
├── TestClient.Spa/         # SPA 测试应用 — 模拟纯前端单页应用
│   ├── index.html          # 首页
│   ├── app.js              # PKCE OAuth 流程
│   ├── config.json         # OAuth 服务器配置
│   └── server.js           # 开发服务器（SPA 路由支持）
├── OAuth.Tests.slnx        # 解决方案文件
└── README.md               # 本文件
```

---

## TestClient.Web — Authorization Code 流程测试（Confidential Client）

模拟第三方 Web 应用（有后端服务，可安全存储 client_secret），通过浏览器交互测试 OAuth 授权码流程。

### 配置

编辑 `appsettings.json`：

```json
{
  "OAuthServer": {
    "BaseUrl": "https://sso.taipi.top",
    "ClientId": "你的 client_id",
    "ClientSecret": "你的 client_secret",
    "RedirectUri": "http://localhost:5002/callback"
  }
}
```

> RedirectUri 需要在 OAuth 服务端的应用白名单中添加。

### 运行

```bash
dotnet run --project test/TestClient.Web
```

启动后访问 `http://localhost:5002`

### 测试清单

| 路由 | 测试场景 | 覆盖要点 |
|---|---|---|
| `/login` | 标准登录 + PKCE | Authorization Code + PKCE 完整流程 |
| `/no-pkce` | 标准登录（无 PKCE） | 仅用 client_secret 换取 Token |
| `/test-scopes` | 自定义 Scope 选择页 | 可选 openid / profile / email 组合 |
| `/login-with-scope?scope=xxx` | 指定 scope 发起登录 | Scope 对 UserInfo 返回字段的影响 |
| `/code-reuse-test` | Code 重复使用测试 | 验证服务端 `CodeUsed` 标记是否生效 |
| `/invalid-secret` | 无效 client_secret 测试 | 先用有效 code，再用错误 secret 换 Token |
| `/test-invalid-token` | 无效 Token 调用 UserInfo | 伪造 JWT 测试服务端鉴权 |
| `/wrong-redirect-uri` | 错误 redirect_uri 测试 | 用错误的回调地址换取 Token，期望被拒绝 |
| `/wrong-code-verifier` | 错误 code_verifier 测试 | 用随机的 code_verifier 换取 Token，期望被拒绝（需先 PKCE 登录） |
| `/cross-client-code` | 跨客户端 Code 测试 | 用 OtherClient 的凭证使用本客户端拿到的 code，期望被拒绝 |
| `/refresh-rotation-test` | Refresh Token 旋转测试 | 刷新后再次使用旧的 refresh_token，期望被拒绝 |
| `/refresh?token=xxx` | Refresh Token 刷新 | 旧 Token 撤销、新 Token 颁发 |
| `/callback?error=xxx` | 拒绝授权错误处理 | 用户 Deny 后的错误回传 |

---

## TestClient.Console — Client Credentials & Token 测试

模拟服务端应用（无用户交互），通过命令行菜单测试 OAuth 客户端凭证流程。

### 配置

直接在 `Program.cs` 顶部修改：

```csharp
var oauthBaseUrl = "https://sso.taipi.top";
var clientId = "your-client-id";
var clientSecret = "your-client-secret";
```

### 运行

```bash
dotnet run --project test/TestClient.Console
```

### 菜单说明

| 选项 | 测试场景 | 覆盖要点 |
|---|---|---|
| [1] | Client Credentials 正常流程 | 获取 Token → 调用 UserInfo |
| [2] | 无效 client_secret | 测试服务端凭证校验 |
| [3] | 不同 Scope 测试 | api / openid / profile / email / unknown_scope |
| [4] | 无效 Token 调用 UserInfo | 伪造 / 空 Token 返回 401 |
| [5] | 手动输入 Token | 从其他途径获取 Token 后验证 |
| [9] | 一键运行全部测试 | 批量执行前 4 项基本测试 |

---

## TestClient.Spa — 纯前端单页应用测试（Public Client）

模拟纯前端单页应用（Vue / React / 原生 JS），**无后端服务**，无法安全存储 client_secret。

### 前置条件

1. 在 OAuth 服务端注册一个 **Public Client**（注册时勾选"公开客户端"）
2. 审核通过后，将 `ClientId` 填入 `config.json`
3. SPA 应用**不需要** client_secret，所有安全通过 PKCE 保障

### 配置

编辑 `config.json`：

```json
{
  "OAuthServer": {
    "BaseUrl": "https://sso.taipi.top",
    "ClientId": "你的 public client_id",
    "RedirectUri": "http://localhost:5003/callback"
  }
}
```

### 运行

用任意 HTTP 服务器托管静态文件：

```bash
node test/TestClient.Spa/server.js
```

启动后访问 `http://localhost:5003`

### 测试清单

| 功能 | 测试场景 | 覆盖要点 |
|---|---|---|
| PKCE 登录 | 标准 PKCE 流程（自动） | 浏览器生成 code_verifier → code_challenge → 换取 Token |
| UserInfo 调用 | 用 access_token 获取用户信息 | Bearer token 鉴权 |
| Code 重放测试 | 重复使用已消费的 code | 期望返回 `invalid_grant` |
| Refresh Token | 刷新 access_token | 旧 token 撤销、新 token 颁发 |
| 拒绝授权 | 用户点击"拒绝" | callback 处理 `error=access_denied` |

---

## 客户端类型对比

| 特性 | TestClient.Web | TestClient.Spa | TestClient.Console |
|---|---|---|---|
| 客户端类型 | Confidential（有机密） | Public（公开） | Confidential（有机密） |
| 能否存 secret | ✅ 是 | ❌ 否 | ✅ 是 |
| 安全机制 | client_secret + PKCE | PKCE 强制 | client_secret |
| Grant Type | authorization_code | authorization_code | client_credentials |
| 用户交互 | ✅ 浏览器 | ✅ 浏览器 | ❌ 纯服务端 |
| 典型场景 | Java / PHP / Python 后端 | Vue / React / 移动端 App | 微服务 / 定时任务 |

---

## OAuth 端点对照

| 端点 | 用途 | 测试方式 |
|---|---|---|
| `GET /connect/authorize` | 授权页（需登录） | Web / SPA 浏览器跳转 |
| `POST /connect/authorize/accept` | 用户同意授权 | Web / SPA 自动处理 |
| `POST /connect/authorize/deny` | 用户拒绝授权 | Web / SPA callback 处理 error 参数 |
| `POST /connect/token` | 换取 / 刷新 Token | 所有项目均覆盖 |
| `GET /connect/userinfo` | 用户信息（需 Bearer token） | 所有项目均覆盖 |

### 支持的 Grant Type

| Grant Type | 覆盖 |
|---|---|
| `authorization_code` (with PKCE) | ✅ Web `/login` + SPA 登录 |
| `authorization_code` (无 PKCE) | ✅ Web `/no-pkce` |
| `client_credentials` | ✅ Console [1] |
| `refresh_token` | ✅ Web `/refresh` + SPA 刷新 |

### 错误场景覆盖

| 场景 | 覆盖 |
|---|---|
| 用户拒绝授权 | ✅ Web / SPA callback error 处理 |
| Code 重复使用 | ✅ Web `/code-reuse-test` + SPA 重放测试 |
| 有效 code + 无效 client_secret | ✅ Web `/invalid-secret` |
| 有效 code + 错误 redirect_uri | ✅ Web `/wrong-redirect-uri` |
| 有效 code + 错误 code_verifier | ✅ Web `/wrong-code-verifier` |
| 跨客户端使用 code | ✅ Web `/cross-client-code` |
| 无效 client_secret（Client Credentials） | ✅ Console [2] |
| 无效 / 伪造 Token | ✅ Web `/test-invalid-token` + Console [4] |
| 无 Authorization Header | ✅ Console [4] 无 Token 测试 |
| 不同 Scope 限制 | ✅ Web `/test-scopes` + Console [3] |
| Public Client 无 secret 换取 Token | ✅ SPA 全程无 secret |
| Refresh Token 刷新 | ✅ Web `/refresh` + SPA |
| Refresh Token 旋转（旧 token 再使用） | ✅ Web `/refresh-rotation-test` |
| 跨客户端使用 Refresh Token | ✅ |

---

## 服务端 Public Client 支持

服务端从以下版本开始支持 Public Client：

### 启用方式

1. 注册应用时设置 `IsPublic = true`（前端勾选"公开客户端"）
2. 审核通过后，该客户端在 Token 换取时可省略 `client_secret`
3. PKCE 仍然是推荐的安全措施

### Token 校验逻辑

```csharp
// authorization_code 流程
if (!client.IsPublic)
{
    // 只有机密客户端才需要校验 client_secret
    if (string.IsNullOrEmpty(clientSecret) || !BCrypt.Verify(clientSecret, hash))
        return invalid_client;
}
// 公开客户端直接放行，走 PKCE 校验

// refresh_token 流程同理
if (!client.IsPublic && (string.IsNullOrEmpty(clientSecret) || !BCrypt.Verify(...)))
    return invalid_client;
```

### 安全说明

Public Client 仍须满足以下条件才能通过审核：

- RedirectUri 必须使用 HTTPS（或 localhost）
- 强烈建议使用 PKCE（code_challenge + code_verifier）
- 服务端会在授权页提示用户该应用为公开客户端

---

## 安全修复

本次增强修复了两个关键的 OAuth 安全漏洞：

### 1. redirect_uri 校验（2019 年之前为可选，RFC 6749 规定为必需）

- 授权请求中的 `redirect_uri` 现在会被存储到 Authorization 记录
- Token 换取时必须传入相同的 `redirect_uri`
- 不一致则返回 `invalid_grant`

**测试方式：** Web `/wrong-redirect-uri` — 用 `http://localhost:9999/fake-callback` 尝试换取 Token，期望被拒绝

### 2. PKCE code_verifier 验证（RFC 7636 规范）

- 授权请求中传了 `code_challenge` 时，Token 请求**必须**带 `code_verifier`
- 服务端对 code_verifier 做 SHA-256 哈希后与 code_challenge 比对
- 校验方法支持 `S256` 和 `plain` 两种模式

**测试方式：** Web `/wrong-code-verifier` — 用随机生成的 code_verifier 换取 Token，期望被拒绝

### 涉及的端点

| 端点 | 变更 |
|---|---|
| `POST /connect/authorize/accept` | 接收并存储 redirect_uri, code_challenge, code_challenge_method |
| `POST /connect/token` | 验证 redirect_uri 匹配 + PKCE code_verifier 校验 |
| `Authorization` 表迁移 | 新增 `RedirectUri`, `CodeChallenge`, `CodeChallengeMethod` 列 |