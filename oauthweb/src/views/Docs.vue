<template>
  <div class="docs-container">
    <nav class="navbar">
      <div class="navbar-inner">
        <router-link to="/" class="navbar-brand">
          <img src="@/assets/logo.png" alt="TP OAuth" class="navbar-logo" />
          <span class="navbar-title">TP OAuth</span>
        </router-link>
        <div class="navbar-right">
          <router-link to="/" class="nav-link">首页</router-link>
          <router-link to="/docs" class="nav-link active">文档</router-link>
          <router-link to="/login" class="nav-link">登录</router-link>
          <router-link to="/register" class="nav-link">注册</router-link>
        </div>
      </div>
    </nav>

    <div class="docs-layout">
      <aside class="docs-sidebar">
        <nav class="sidebar-nav">
          <a v-for="item in sidebarItems" :key="item.id" :href="'#' + item.id" class="sidebar-link" :class="{ active: activeSection === item.id }">{{ item.title }}</a>
        </nav>
      </aside>

      <main class="docs-content">
        <h1 id="overview">开发文档</h1>
        <p class="lead">TP OAuth 是一个标准的 OAuth 2.0 / OpenID Connect 授权服务。本文档介绍如何将您的应用接入本平台。</p>

        <section id="quickstart">
          <h2>快速开始</h2>
          <h3>1. 注册客户端</h3>
          <p>在 <router-link to="/apps">我的应用</router-link> 页面点击"注册新应用"，填写应用信息并提交审核。审核通过后即可获取凭证。</p>
          <h3>2. 获取凭证</h3>
          <p>审核通过后，在应用详情页可获得以下凭证：</p>
          <table class="param-table">
            <thead>
              <tr><th>参数</th><th>说明</th></tr>
            </thead>
            <tbody>
              <tr><td><code>Client ID</code></td><td>应用唯一标识，公开展示</td></tr>
              <tr><td><code>Client Secret</code></td><td>应用密钥，请妥善保管，仅机密客户端使用</td></tr>
            </tbody>
          </table>
        </section>

        <section id="flows">
          <h2>授权流程</h2>

          <h3>授权码模式（推荐）</h3>
          <p>适用于有后端的 Web 应用，是最安全的 OAuth 2.0 流程。</p>
          <ol class="flow-list">
            <li>用户访问您的应用，点击"使用 TP OAuth 登录"</li>
            <li>您的应用将用户重定向到 TP OAuth 授权页面：<br/>
              <code class="block">GET /connect/authorize?client_id={client_id}&redirect_uri={redirect_uri}&response_type=code&scope=openid%20profile&state={random_state}</code>
            </li>
            <li>用户确认授权后，浏览器重定向回您的应用，并携带授权码 <code>code</code></li>
            <li>您的后端使用授权码向 Token 端点换取令牌：<br/>
              <code class="block">POST /connect/token<br/>
Content-Type: application/x-www-form-urlencoded<br/><br/>
grant_type=authorization_code&code={code}&redirect_uri={redirect_uri}&client_id={client_id}&client_secret={client_secret}</code>
            </li>
            <li>返回 <code>access_token</code>、<code>refresh_token</code> 和可选的 <code>id_token</code></li>
          </ol>

          <h3>PKCE 模式（移动端 / SPA）</h3>
          <p>适用于无法安全保存 Client Secret 的应用（如移动 App、单页应用）。</p>
          <ol class="flow-list">
            <li>前端生成一个随机 <code>code_verifier</code> 和对应的 <code>code_challenge</code>（SHA-256 哈希的 Base64URL 编码）</li>
            <li>将用户重定向到授权页面，额外携带 PKCE 参数：<br/>
              <code class="block">GET /connect/authorize?client_id={client_id}&redirect_uri={redirect_uri}&response_type=code&scope=openid%20profile&state={state}&code_challenge={code_challenge}&code_challenge_method=S256</code>
            </li>
            <li>用户授权后获取 <code>code</code>，换令牌时无需 <code>client_secret</code>，但需提供 <code>code_verifier</code></li>
          </ol>
        </section>

        <section id="endpoints">
          <h2>接口说明</h2>

          <table class="param-table">
            <thead>
              <tr><th>端点</th><th>方法</th><th>说明</th></tr>
            </thead>
            <tbody>
              <tr><td><code>/connect/authorize</code></td><td>GET</td><td>授权端点，将用户重定向到此 URL</td></tr>
              <tr><td><code>/connect/token</code></td><td>POST</td><td>令牌端点，用于获取/刷新令牌</td></tr>
              <tr><td><code>/connect/userinfo</code></td><td>GET</td><td>用户信息端点，使用 access_token 获取用户信息</td></tr>
              <tr><td><code>/connect/endsession</code></td><td>GET</td><td>登出端点</td></tr>
            </tbody>
          </table>
        </section>

        <section id="params">
          <h2>参数说明</h2>

          <table class="param-table">
            <thead>
              <tr><th>参数</th><th>必填</th><th>说明</th></tr>
            </thead>
            <tbody>
              <tr><td><code>client_id</code></td><td>是</td><td>应用唯一标识，在客户端注册后获得</td></tr>
              <tr><td><code>redirect_uri</code></td><td>是</td><td>回调地址，必须与注册时填写的一致</td></tr>
              <tr><td><code>response_type</code></td><td>是</td><td>固定为 <code>code</code></td></tr>
              <tr><td><code>scope</code></td><td>是</td><td>请求的权限范围，空格分隔。可选值：<code>openid</code>、<code>profile</code>、<code>email</code>、<code>phone</code></td></tr>
              <tr><td><code>state</code></td><td>推荐</td><td>用于防止 CSRF 攻击，回调时会原样返回</td></tr>
              <tr><td><code>code_challenge</code></td><td>PKCE 时必填</td><td>对 code_verifier 进行 SHA-256 哈希后的 Base64URL 编码值</td></tr>
              <tr><td><code>code_challenge_method</code></td><td>PKCE 时必填</td><td>固定为 <code>S256</code></td></tr>
            </tbody>
          </table>
        </section>

        <section id="scopes">
          <h2>权限范围说明</h2>

          <table class="param-table">
            <thead>
              <tr><th>Scope</th><th>返回信息</th></tr>
            </thead>
            <tbody>
              <tr><td><code>openid</code></td><td>OpenID Connect 身份验证，返回 <code>id_token</code> 和 <code>sub</code>（用户唯一标识）</td></tr>
              <tr><td><code>profile</code></td><td>用户基本信息：昵称、头像等</td></tr>
              <tr><td><code>email</code></td><td>用户邮箱地址</td></tr>
              <tr><td><code>phone</code></td><td>用户手机号码</td></tr>
            </tbody>
          </table>
        </section>

        <section id="examples">
          <h2>代码示例</h2>

          <h3>cURL</h3>
          <pre><code># 授权码模式 - 获取 Token
curl -X POST https://ssoapi.taipi.top/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code" \
  -d "code={authorization_code}" \
  -d "redirect_uri=https://your-app.com/callback" \
  -d "client_id=your-client-id" \
  -d "client_secret=your-client-secret"

# 使用 access_token 获取用户信息
curl -H "Authorization: Bearer {access_token}" \
  https://ssoapi.taipi.top/connect/userinfo

# 刷新令牌
curl -X POST https://ssoapi.taipi.top/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token" \
  -d "refresh_token={refresh_token}" \
  -d "client_id=your-client-id"</code></pre>

          <h3>JavaScript (Node.js)</h3>
          <pre><code>const axios = require('axios');

// 获取 Token
async function getToken(code, redirectUri) {
  const params = new URLSearchParams({
    grant_type: 'authorization_code',
    code,
    redirect_uri: redirectUri,
    client_id: process.env.CLIENT_ID,
    client_secret: process.env.CLIENT_SECRET
  });

  const res = await axios.post('https://ssoapi.taipi.top/connect/token', params, {
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
  });

  return res.data; // { access_token, refresh_token, id_token, expires_in }
}

// 获取用户信息
async function getUserInfo(accessToken) {
  const res = await axios.get('https://ssoapi.taipi.top/connect/userinfo', {
    headers: { Authorization: `Bearer ${accessToken}` }
  });
  return res.data;
}</code></pre>

          <h3>C# (.NET)</h3>
          <pre><code>using var httpClient = new HttpClient();

// 获取 Token
var tokenParams = new Dictionary&lt;string, string&gt;
{
    ["grant_type"] = "authorization_code",
    ["code"] = authorizationCode,
    ["redirect_uri"] = redirectUri,
    ["client_id"] = clientId,
    ["client_secret"] = clientSecret
};

var tokenResponse = await httpClient.PostAsync(
    "https://ssoapi.taipi.top/connect/token",
    new FormUrlEncodedContent(tokenParams)
);
var tokenData = await tokenResponse.Content.ReadFromJsonAsync&lt;TokenResponse&gt;();

// 获取用户信息
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
var userInfo = await httpClient.GetFromJsonAsync&lt;UserInfoResponse&gt;(
    "https://ssoapi.taipi.top/connect/userinfo"
);</code></pre>
        </section>

        <section id="security">
          <h2>安全建议</h2>
          <ul class="security-list">
            <li><strong>使用 HTTPS</strong> — 所有接口必须走 HTTPS，防止中间人攻击</li>
            <li><strong>验证 state 参数</strong> — 在发起授权请求时生成随机 state，回调时验证，防止 CSRF</li>
            <li><strong>PKCE 优先</strong> — 即使是服务端应用，也建议使用 PKCE 增强安全性</li>
            <li><strong>限制 Scope</strong> — 只申请应用真正需要的权限，最小化数据访问</li>
            <li><strong>保护 Client Secret</strong> — 服务器端存储，不要暴露在客户端代码中</li>
            <li><strong>定期轮换密钥</strong> — 定期更新 Client Secret，降低泄露风险</li>
            <li><strong>验证 redirect_uri</strong> — 确保回调地址与注册时一致，防止授权码被拦截</li>
          </ul>
        </section>

        <section id="open-source">
            <h2>开源</h2>
            <p>TP OAuth 是一个开源项目，基于 .NET 10 和 Vue 3 开发。</p>
            <p>GitHub 仓库地址：
              <a href="https://github.com/koeltp/oauth.git" target="_blank" rel="noopener noreferrer">
                https://github.com/koeltp/oauth.git
              </a>
            </p>
            <p>欢迎贡献代码、提交 Issue 或 Star 支持。</p>
          </section>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'

const activeSection = ref('')

const sidebarItems = [
  { id: 'overview', title: '概述' },
  { id: 'quickstart', title: '快速开始' },
  { id: 'flows', title: '授权流程' },
  { id: 'endpoints', title: '接口说明' },
  { id: 'params', title: '参数说明' },
  { id: 'scopes', title: '权限范围' },
  { id: 'examples', title: '代码示例' },
  { id: 'security', title: '安全建议' },
  { id: 'open-source', title: '开源' }
]

onMounted(() => {
  const observer = new IntersectionObserver(
    (entries) => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          activeSection.value = entry.target.id
        }
      }
    },
    { rootMargin: '-80px 0px -60% 0px' }
  )

  for (const item of sidebarItems) {
    const el = document.getElementById(item.id)
    if (el) observer.observe(el)
  }
})
</script>

<style scoped>
.docs-container {
  min-height: 100vh;
  background: #f5f7fa;
}

.navbar {
  display: flex;
  align-items: center;
  height: 60px;
  border-bottom: 1px solid #f0f0f0;
  position: sticky;
  top: 0;
  background: white;
  z-index: 100;
}

.navbar-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 20px;
}

.navbar-brand {
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
}

.navbar-logo {
  height: 32px;
}

.navbar-title {
  font-size: 18px;
  font-weight: 600;
  color: #333;
}

.navbar-right {
  display: flex;
  align-items: center;
  gap: 24px;
}

.nav-link {
  font-size: 15px;
  color: #666;
  text-decoration: none;
  transition: color 0.2s;
}

.nav-link:hover,
.nav-link.active {
  color: #409eff;
  font-weight: 500;
}

.docs-layout {
  display: flex;
  max-width: 1200px;
  margin: 0 auto;
  padding: 30px 20px;
  gap: 30px;
}

.docs-sidebar {
  width: 200px;
  flex-shrink: 0;
  position: sticky;
  top: 90px;
  align-self: flex-start;
}

.sidebar-nav {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.sidebar-link {
  display: block;
  padding: 8px 12px;
  font-size: 14px;
  color: #666;
  text-decoration: none;
  border-radius: 6px;
  transition: all 0.2s;
}

.sidebar-link:hover {
  background: #e8f0fe;
  color: #409eff;
}

.sidebar-link.active {
  background: #e8f0fe;
  color: #409eff;
  font-weight: 500;
}

.docs-content {
  flex: 1;
  min-width: 0;
  background: white;
  padding: 40px;
  border-radius: 12px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.06);
}

.docs-content h1 {
  font-size: 32px;
  font-weight: 700;
  margin-bottom: 16px;
  color: #333;
}

.docs-content .lead {
  font-size: 16px;
  color: #666;
  margin-bottom: 40px;
  line-height: 1.6;
}

.docs-content h2 {
  font-size: 24px;
  font-weight: 600;
  margin: 40px 0 20px;
  padding-bottom: 10px;
  border-bottom: 1px solid #f0f0f0;
  color: #333;
}

.docs-content h3 {
  font-size: 18px;
  font-weight: 600;
  margin: 24px 0 12px;
  color: #333;
}

.docs-content p {
  font-size: 15px;
  line-height: 1.8;
  color: #444;
  margin-bottom: 12px;
}

.docs-content ol,
.docs-content ul {
  margin-bottom: 16px;
  padding-left: 24px;
}

.docs-content li {
  font-size: 15px;
  line-height: 1.8;
  color: #444;
  margin-bottom: 8px;
}

.flow-list li {
  margin-bottom: 16px;
}

.param-table {
  width: 100%;
  border-collapse: collapse;
  margin-bottom: 20px;
  font-size: 14px;
}

.param-table th,
.param-table td {
  padding: 10px 14px;
  text-align: left;
  border: 1px solid #e4e7ed;
}

.param-table th {
  background: #f5f7fa;
  font-weight: 600;
  color: #333;
}

.param-table td {
  color: #444;
}

code {
  font-family: 'Cascadia Code', 'Fira Code', Consolas, monospace;
  font-size: 13px;
  background: #f5f7fa;
  padding: 2px 6px;
  border-radius: 4px;
  color: #d63384;
}

code.block {
  display: block;
  white-space: pre-wrap;
  padding: 12px 16px;
  margin: 8px 0;
  background: #f8f9fa;
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  color: #333;
  line-height: 1.6;
}

pre {
  background: #f8f9fa;
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  padding: 16px;
  overflow-x: auto;
  margin-bottom: 16px;
}

pre code {
  background: none;
  padding: 0;
  color: #333;
  font-size: 13px;
  line-height: 1.6;
}

.security-list li {
  margin-bottom: 12px;
}

.docs-content a {
  color: #409eff;
  text-decoration: none;
}

.docs-content a:hover {
  text-decoration: underline;
}

@media (max-width: 768px) {
  .docs-sidebar {
    display: none;
  }
  .docs-content {
    padding: 24px;
  }
  .navbar {
    padding: 0 16px;
  }
}
</style>