// ==================== PKCE 工具函数 ====================

function base64URLEncode(buffer) {
    return btoa(String.fromCharCode(...new Uint8Array(buffer)))
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
}

async function generateCodeVerifier() {
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    return base64URLEncode(array);
}

async function generateCodeChallenge(verifier) {
    const encoder = new TextEncoder();
    const data = encoder.encode(verifier);
    const digest = await crypto.subtle.digest('SHA-256', data);
    return base64URLEncode(digest);
}

async function loadConfig() {
    const resp = await fetch('config.json');
    return await resp.json();
}

// ==================== 路由处理 ====================

const path = window.location.pathname;
const params = new URLSearchParams(window.location.search);
let config;

(async () => {
    try {
        config = await loadConfig();
    } catch (e) {
        document.getElementById('app').innerHTML = `
            <div class="card error-card">
                <h2>配置加载失败</h2>
                <p>请确保 config.json 文件存在且格式正确。</p>
                <pre>${e.message}</pre>
            </div>`;
        return;
    }

    if (path === '/' || path === '/index.html') {
        renderHome();
    } else if (path === '/login') {
        await startLogin(false);
    } else if (path === '/login-no-pkce') {
        await startLogin(true);
    } else if (path === '/callback') {
        await handleCallback();
    }
})();

// ==================== 首页 ====================

function renderHome() {
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="card">
            <h1>OAuth SPA 测试客户端</h1>
            <p class="subtitle">模拟单页应用（纯前端）通过 OAuth 登录</p>

            <div class="info-box">
                <strong>认证方式：</strong>Authorization Code + PKCE
                <br><br>
                <strong>说明：</strong>
                <ul>
                    <li>SPA 无法安全存储 <code>client_secret</code>，因此使用 PKCE 增强安全</li>
                    <li>服务端对应 <strong>Public Client</strong> 类型，无需 secret</li>
                    <li>所有密钥在浏览器端生成，仅用于本次会话</li>
                </ul>
            </div>

            <div class="config-display">
                <strong>当前配置</strong>
                <table>
                    <tr><td>OAuth 服务器</td><td>${escapeHtml(config.OAuthServer.BaseUrl)}</td></tr>
                    <tr><td>Client ID</td><td>${escapeHtml(config.OAuthServer.ClientId)}</td></tr>
                    <tr><td>回调地址</td><td>${escapeHtml(config.OAuthServer.RedirectUri)}</td></tr>
                </table>
            </div>

            <div class="btn-group">
                <button onclick="window.location.href='/login'" class="btn btn-primary">
                    PKCE 登录（标准流程）
                </button>
                <button onclick="window.location.href='/login-no-pkce'" class="btn btn-secondary">
                    无 PKCE 登录（仅演示）
                </button>
            </div>
        </div>

        <div class="card">
            <h2>测试结果</h2>
            <div id="resultArea">
                ${sessionStorage.getItem('lastResult') || '<p class="muted">暂无测试结果</p>'}
            </div>
            ${sessionStorage.getItem('lastResult') ? '<button onclick="clearResult()" class="btn btn-small">清除结果</button>' : ''}
        </div>
    `;
}

function clearResult() {
    sessionStorage.removeItem('lastResult');
    renderHome();
}

// ==================== 发起登录 ====================

async function startLogin(noPkce) {
    const app = document.getElementById('app');
    app.innerHTML = `<div class="card"><h2>正在跳转到 OAuth 授权页...</h2></div>`;

    const state = generateRandomState();
    sessionStorage.setItem('oauth_state', state);

    const baseUrl = config.OAuthServer.BaseUrl;
    const clientId = config.OAuthServer.ClientId;
    const redirectUri = config.OAuthServer.RedirectUri;
    const scope = params.get('scope') || 'openid profile email';

    let authorizeUrl = `${baseUrl}/connect/authorize` +
        `?response_type=code` +
        `&client_id=${encodeURIComponent(clientId)}` +
        `&redirect_uri=${encodeURIComponent(redirectUri)}` +
        `&scope=${encodeURIComponent(scope)}` +
        `&state=${state}`;

    if (!noPkce) {
        const codeVerifier = await generateCodeVerifier();
        const codeChallenge = await generateCodeChallenge(codeVerifier);

        sessionStorage.setItem('code_verifier', codeVerifier);

        authorizeUrl += `&code_challenge=${codeChallenge}` +
            `&code_challenge_method=S256`;
    } else {
        sessionStorage.setItem('no_pkce', 'true');
    }

    window.location.href = authorizeUrl;
}

// ==================== 回调处理 ====================

async function handleCallback() {
    const app = document.getElementById('app');
    app.innerHTML = `<div class="card"><h2>正在处理授权回调...</h2></div>`;

    const code = params.get('code');
    const state = params.get('state');
    const error = params.get('error');
    const errorDescription = params.get('error_description');

    // 检查错误（用户拒绝）
    if (error) {
        app.innerHTML = `
            <div class="card error-card">
                <h2>授权失败</h2>
                <p><strong>错误：</strong>${escapeHtml(error)}</p>
                <p><strong>描述：</strong>${escapeHtml(errorDescription || '用户拒绝了授权请求')}</p>
                <button onclick="window.location.href='/'" class="btn">返回首页</button>
            </div>`;
        return;
    }

    // 校验 state
    const savedState = sessionStorage.getItem('oauth_state');
    if (!state || state !== savedState) {
        app.innerHTML = `
            <div class="card error-card">
                <h2>State 校验失败</h2>
                <p>CSRF 攻击已被拦截（state 参数不匹配）</p>
                <button onclick="window.location.href='/'" class="btn">返回首页</button>
            </div>`;
        return;
    }
    sessionStorage.removeItem('oauth_state');

    // 换取 Token
    const codeVerifier = sessionStorage.getItem('code_verifier');
    const noPkce = sessionStorage.getItem('no_pkce') === 'true';
    sessionStorage.removeItem('code_verifier');
    sessionStorage.removeItem('no_pkce');

    const baseUrl = config.OAuthServer.BaseUrl;
    const clientId = config.OAuthServer.ClientId;
    const redirectUri = config.OAuthServer.RedirectUri;

    try {
        const tokenParams = new URLSearchParams();
        tokenParams.append('grant_type', 'authorization_code');
        tokenParams.append('code', code);
        tokenParams.append('redirect_uri', redirectUri);
        tokenParams.append('client_id', clientId);

        if (codeVerifier) {
            tokenParams.append('code_verifier', codeVerifier);
        }

        const tokenResp = await fetch(`${baseUrl}/connect/token`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: tokenParams
        });

        const tokenBody = await tokenResp.json();

        if (!tokenResp.ok) {
            app.innerHTML = `
                <div class="card error-card">
                    <h2>Token 换取失败</h2>
                    <p><strong>状态码：</strong>${tokenResp.status}</p>
                    <p><strong>错误：</strong>${escapeHtml(tokenBody.error || '未知错误')}</p>
                    <p><strong>描述：</strong>${escapeHtml(tokenBody.error_description || '')}</p>
                    <p><strong>PKCE：</strong>${codeVerifier ? '是' : '否'}</p>
                    <button onclick="window.location.href='/'" class="btn">返回首页</button>
                </div>`;
            return;
        }

        // 显示成功结果
        const accessToken = tokenBody.access_token;
        const tokenType = tokenBody.token_type || 'Bearer';
        const expiresIn = tokenBody.expires_in || '?';
        const scope = tokenBody.scope || '无';
        const refreshToken = tokenBody.refresh_token || '';

        let resultHtml = `
            <div class="card success-card">
                <h2>Token 获取成功</h2>
                <p><strong>PKCE：</strong>${codeVerifier ? '是' : '否'}</p>
                <p><strong>Token 类型：</strong>${escapeHtml(tokenType)}</p>
                <p><strong>过期时间：</strong>${expiresIn} 秒</p>
                <p><strong>Scope：</strong>${escapeHtml(scope)}</p>
                <div class="token-box">
                    <strong>access_token：</strong>
                    <code>${escapeHtml(accessToken)}</code>
                </div>
                ${refreshToken ? `
                <div class="token-box">
                    <strong>refresh_token：</strong>
                    <code>${escapeHtml(refreshToken)}</code>
                </div>
                <button onclick="refreshToken('${escapeHtml(refreshToken)}')" class="btn btn-small">刷新 Token</button>
                ` : ''}
            </div>

            <div class="card">
                <h3>调用 UserInfo</h3>
                <button onclick="callUserInfo('${escapeHtml(accessToken)}', '${escapeHtml(tokenType)}')" class="btn">
                    获取用户信息
                </button>
                <div id="userInfoResult"></div>
            </div>

            <div class="card">
                <h3>Code 重复使用测试</h3>
                <p>使用相同的 authorization code 再次换取 Token，期望服务端返回 <code>invalid_grant</code></p>
                <button onclick="replayCode()" class="btn btn-warning">
                    重放 Code
                </button>
                <div id="replayResult"></div>
            </div>
        `;

        app.innerHTML = resultHtml;

        // 保存结果到 sessionStorage
        const summary = `
            <p><strong>✓ PKCE ${codeVerifier ? '是' : '否'} — Token 获取成功</strong></p>
            <p>Scope: ${escapeHtml(scope)} | 过期: ${expiresIn}s</p>`;
        sessionStorage.setItem('lastResult', summary);

        // 保存 code 供重放测试
        sessionStorage.setItem('last_auth_code', code);

    } catch (e) {
        app.innerHTML = `
            <div class="card error-card">
                <h2>网络错误</h2>
                <p>${escapeHtml(e.message)}</p>
                <button onclick="window.location.href='/'" class="btn">返回首页</button>
            </div>`;
    }
}

// ==================== 调用 UserInfo ====================

async function callUserInfo(token, tokenType) {
    const div = document.getElementById('userInfoResult');
    div.innerHTML = '<p>请求中...</p>';

    try {
        const resp = await fetch(`${config.OAuthServer.BaseUrl}/connect/userinfo`, {
            headers: { 'Authorization': `${tokenType} ${token}` }
        });
        const body = await resp.json();

        if (resp.ok) {
            div.innerHTML = `<pre class="success">${escapeHtml(JSON.stringify(body, null, 2))}</pre>`;
        } else {
            div.innerHTML = `
                <div class="error">
                    <p>状态码: ${resp.status}</p>
                    <pre>${escapeHtml(JSON.stringify(body, null, 2))}</pre>
                </div>`;
        }
    } catch (e) {
        div.innerHTML = `<div class="error"><p>${escapeHtml(e.message)}</p></div>`;
    }
}

// ==================== 刷新 Token ====================

async function refreshToken(refreshTokenValue) {
    const container = document.createElement('div');
    document.getElementById('app').appendChild(container);
    container.innerHTML = '<div class="card"><h3>正在刷新 Token...</h3></div>';

    try {
        const params = new URLSearchParams();
        params.append('grant_type', 'refresh_token');
        params.append('refresh_token', refreshTokenValue);
        params.append('client_id', config.OAuthServer.ClientId);

        const resp = await fetch(`${config.OAuthServer.BaseUrl}/connect/token`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: params
        });
        const body = await resp.json();

        if (resp.ok) {
            container.innerHTML = `
                <div class="card success-card">
                    <h3>Token 刷新成功</h3>
                    <p>新的 access_token 已颁发，旧的 refresh_token 已撤销</p>
                    <pre>${escapeHtml(JSON.stringify(body, null, 2))}</pre>
                </div>`;
        } else {
            container.innerHTML = `
                <div class="card error-card">
                    <h3>Token 刷新失败</h3>
                    <pre>${escapeHtml(JSON.stringify(body, null, 2))}</pre>
                </div>`;
        }
    } catch (e) {
        container.innerHTML = `<div class="card error-card"><p>${escapeHtml(e.message)}</p></div>`;
    }
}

// ==================== Code 重放测试 ====================

async function replayCode() {
    const div = document.getElementById('replayResult');
    if (!div) return;

    const code = sessionStorage.getItem('last_auth_code');
    if (!code) {
        div.innerHTML = '<div class="error"><p>没有可重放的 code</p></div>';
        return;
    }

    div.innerHTML = '<p>正在用相同的 code 重新换取 Token...</p>';

    try {
        const params = new URLSearchParams();
        params.append('grant_type', 'authorization_code');
        params.append('code', code);
        params.append('redirect_uri', config.OAuthServer.RedirectUri);
        params.append('client_id', config.OAuthServer.ClientId);

        const resp = await fetch(`${config.OAuthServer.BaseUrl}/connect/token`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: params
        });
        const body = await resp.json();

        if (resp.ok) {
            div.innerHTML = `
                <div class="warning">
                    <p>注意！Code 竟然还能用（状态码 ${resp.status}）</p>
                    <pre>${escapeHtml(JSON.stringify(body, null, 2))}</pre>
                    <p>服务端未正确标记 code 为已使用</p>
                </div>`;
        } else {
            div.innerHTML = `
                <div class="success">
                    <p>正确！服务端拒绝了重复使用的 code（状态码 ${resp.status}）</p>
                    <pre>${escapeHtml(JSON.stringify(body, null, 2))}</pre>
                </div>`;
        }
    } catch (e) {
        div.innerHTML = `<div class="error"><p>${escapeHtml(e.message)}</p></div>`;
    }
}

// ==================== 工具函数 ====================

function generateRandomState() {
    const array = new Uint8Array(16);
    crypto.getRandomValues(array);
    return base64URLEncode(array);
}

function escapeHtml(str) {
    if (str == null) return '';
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}