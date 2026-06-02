# OAuth Server

基于 .NET 8 + OpenIddict + Vue 3 的 OAuth 授权服务器

## 项目结构

```
oauthapi/           # .NET 8 后端
├── OAuth.Server/   # Web API 项目
├── OAuth.Domain/   # 领域实体
├── OAuth.Application/  # 应用服务
└── OAuth.Infrastructure/  # 基础设施

oauthweb/          # Vue 3 前端
```

## 快速开始

### 后端

```bash
cd oauthapi
dotnet restore
dotnet run --project OAuth.Server
```

### 数据库迁移
```
cd PS D:\work\prod\oauth\oauthapi>
dotnet ef database update --project OAuth.Infrastructure --startup-project OAuth.Server
```

### 前端

```bash
cd oauthweb
npm install
npm run dev
```

### Docker

```bash
docker-compose up -d
```

## 功能特性

- ✅ OAuth 2.0 + OpenID Connect
- ✅ 多种登录方式（密码、邮箱验证码、短信验证码）
- ✅ 两步验证 (2FA)
- ✅ 第三方登录（GitHub、微信）
- ✅ 客户端审核流程
- ✅ JWT 令牌
- ✅ PKCE 支持（移动端）
