using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

// ==================== HTML 模板 ====================
const string HomePage = """
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>第三方应用 - OAuth 测试客户端</title>
    <style>
        body { font-family: sans-serif; padding: 32px; background: #f5f7fa; }
        .card { background: white; padding: 32px; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.08); max-width: 700px; margin: 0 auto; }
        h1 { color: #333; text-align: center; }
        .desc { color: #666; text-align: center; margin-bottom: 24px; }
        .test-grid { display: flex; flex-direction: column; gap: 12px; }
        .test-item { display: flex; align-items: center; justify-content: space-between; padding: 16px; border: 1px solid #eee; border-radius: 8px; }
        .test-item .label { color: #333; font-weight: 500; }
        .test-item .desc-sm { color: #999; font-size: 13px; }
        .btn { padding: 8px 20px; background: #409eff; color: white; border-radius: 6px; text-decoration: none; font-size: 14px; white-space: nowrap; }
        .btn-warn { background: #e6a23c; }
        .btn-danger { background: #f56c6c; }
        .btn:hover { opacity: 0.85; }
    </style>
</head>
<body>
    <div class="card">
        <h1>OAuth 第三方测试客户端</h1>
        <p class="desc">模拟第三方应用，测试 OAuth Authorization Code 各种场景</p>
        <div class="test-grid">
            <div class="test-item">
                <div>
                    <div class="label">标准登录（PKCE）</div>
                    <div class="desc-sm">完整的 Authorization Code + PKCE 流程</div>
                </div>
                <a class="btn" href="/login">测试</a>
            </div>
            <div class="test-item">
                <div>
                    <div class="label">标准登录（无 PKCE）</div>
                    <div class="desc-sm">仅用 client_secret 换取 Token</div>
                </div>
                <a class="btn" href="/no-pkce">测试</a>
            </div>
            <div class="test-item">
                <div>
                    <div class="label">自定义 Scope</div>
                    <div class="desc-sm">测试不同的 scope 参数对返回结果的影响</div>
                </div>
                <a class="btn btn-warn" href="/test-scopes">测试</a>
            </div>
            <div class="test-item">
                <div>
                    <div class="label">Code 重复使用</div>
                    <div class="desc-sm">模拟重复使用已消费的 authorization code</div>
                </div>
                <a class="btn btn-warn" href="/code-reuse-test">测试</a>
            </div>
            <div class="test-item">
                <div>
                    <div class="label">无效 client_secret</div>
                    <div class="desc-sm">使用错误的 client_secret 换取 Token</div>
                </div>
                <a class="btn btn-danger" href="/invalid-secret">测试</a>
            </div>
            <div class="test-item">
                <div>
                    <div class="label">无效 Token 调用 UserInfo</div>
                    <div class="desc-sm">使用伪造的 Bearer token 访问 /userinfo</div>
                </div>
                <a class="btn btn-danger" href="/test-invalid-token">测试</a>
            </div>
            <div class="test-item">
                <div>
                    <div class="label">错误 redirect_uri</div>
                    <div class="desc-sm">用错误的 redirect_uri 换取 Token</div>
                </div>
                <a class="btn btn-danger" href="/wrong-redirect-uri">测试</a>
            </div>
            <div class="test-item">
                <div>
                    <div class="label">错误 code_verifier</div>
                    <div class="desc-sm">用错误的 code_verifier 换取 Token（需先 PKCE 登录）</div>
                </div>
                <a class="btn btn-danger" href="/wrong-code-verifier">测试</a>
            </div>
            <div class="test-item">
                <div>
                    <div class="label">跨客户端 Code</div>
                    <div class="desc-sm">用别的客户端的凭证来使用本客户端拿到的 code</div>
                </div>
                <a class="btn btn-danger" href="/cross-client-code">测试</a>
            </div>
            <div class="test-item">
                <div>
                    <div class="label">Refresh Token 旋转</div>
                    <div class="desc-sm">刷新后再次使用旧的 refresh_token</div>
                </div>
                <a class="btn btn-warn" href="/refresh-rotation-test">测试</a>
            </div>
        </div>
    </div>
</body>
</html>
""";

const string ResultPage = """
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>{title}</title>
    <style>
        body { font-family: sans-serif; padding: 32px; background: #f5f7fa; }
        .card { background: white; padding: 32px; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.08); max-width: 800px; margin: 0 auto; }
        h2 { color: {headingColor}; margin-bottom: 16px; }
        pre { background: #f8f9fa; padding: 16px; border-radius: 8px; overflow-x: auto; font-size: 13px; }
        .section { margin-bottom: 24px; }
        .section h3 { color: #333; border-bottom: 1px solid #eee; padding-bottom: 8px; }
        .btn { display: inline-block; padding: 10px 24px; background: #409eff; color: white; border-radius: 6px; text-decoration: none; }
        .btn-back { background: #909399; }
        .nav-links { margin-top: 20px; display: flex; gap: 10px; }
    </style>
</head>
<body>
    <div class="card">
        {content}
        <div class="nav-links">
            <a class="btn btn-back" href="/">返回首页</a>
        </div>
    </div>
</body>
</html>
""";

// ==================== 启动 ====================
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();
app.UseSession();
app.UseDeveloperExceptionPage();

var oauth = app.Configuration.GetSection("OAuthServer");
var oauthBaseUrl = oauth["BaseUrl"] ?? "https://sso.taipi.top";
var clientId = oauth["ClientId"] ?? "test-client-id";
var clientSecret = oauth["ClientSecret"] ?? "test-client-secret";
var redirectUri = oauth["RedirectUri"] ?? "http://localhost:5002/callback";
var otherClientId = oauth["OtherClientId"] ?? clientId;
var otherClientSecret = oauth["OtherClientSecret"] ?? clientSecret;

var httpClientFactory = new HttpClient
{
    DefaultRequestHeaders = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
};

// ==================== 首页 ====================
app.MapGet("/", () => Results.Content(HomePage, "text/html; charset=utf-8"));

// ==================== 1. 标准登录（PKCE） ====================
app.MapGet("/login", async (HttpContext ctx) =>
{
    var codeVerifier = GenerateCodeVerifier();
    var codeChallenge = GenerateCodeChallenge(codeVerifier);

    ctx.Session.SetString("code_verifier", codeVerifier);
    ctx.Session.SetString("use_pkce", "true");

    var state = Guid.NewGuid().ToString("N");
    ctx.Session.SetString("oauth_state", state);

    var authorizeUrl = $"{oauthBaseUrl}/connect/authorize" +
        $"?response_type=code" +
        $"&client_id={HttpUtility.UrlEncode(clientId)}" +
        $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
        $"&scope=openid profile email" +
        $"&state={state}" +
        $"&code_challenge={codeChallenge}" +
        $"&code_challenge_method=S256";

    return Results.Redirect(authorizeUrl);
});

// ==================== 2. 标准登录（无 PKCE） ====================
app.MapGet("/no-pkce", async (HttpContext ctx) =>
{
    ctx.Session.SetString("use_pkce", "false");

    var state = Guid.NewGuid().ToString("N");
    ctx.Session.SetString("oauth_state", state);

    var authorizeUrl = $"{oauthBaseUrl}/connect/authorize" +
        $"?response_type=code" +
        $"&client_id={HttpUtility.UrlEncode(clientId)}" +
        $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
        $"&scope=openid profile email" +
        $"&state={state}";

    return Results.Redirect(authorizeUrl);
});

// ==================== 3. 自定义 Scope ====================
var scopePage = ResultPage
    .Replace("{title}", "自定义 Scope 测试")
    .Replace("{content}", """
        <h2 style="color:#e6a23c;">自定义 Scope 测试</h2>
        <p>选择要测试的 scope：</p>
        <p><a class="btn" href="/login-with-scope?scope=openid">仅 openid</a></p>
        <p><a class="btn" href="/login-with-scope?scope=openid%20profile">openid + profile</a></p>
        <p><a class="btn" href="/login-with-scope?scope=openid%20email">openid + email</a></p>
        <p><a class="btn" href="/login-with-scope?scope=openid%20profile%20email">openid + profile + email</a></p>
        <p><a class="btn btn-warn" href="/login-with-scope?scope=api">仅 api（错误 scope）</a></p>
    """);

app.MapGet("/test-scopes", () => Results.Content(scopePage, "text/html; charset=utf-8"));

app.MapGet("/login-with-scope", async (HttpContext ctx) =>
{
    var scope = ctx.Request.Query["scope"].FirstOrDefault() ?? "openid";
    var codeVerifier = GenerateCodeVerifier();
    var codeChallenge = GenerateCodeChallenge(codeVerifier);

    ctx.Session.SetString("code_verifier", codeVerifier);
    ctx.Session.SetString("use_pkce", "true");

    var state = Guid.NewGuid().ToString("N");
    ctx.Session.SetString("oauth_state", state);

    var authorizeUrl = $"{oauthBaseUrl}/connect/authorize" +
        $"?response_type=code" +
        $"&client_id={HttpUtility.UrlEncode(clientId)}" +
        $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
        $"&scope={HttpUtility.UrlEncode(scope)}" +
        $"&state={state}" +
        $"&code_challenge={codeChallenge}" +
        $"&code_challenge_method=S256";

    return Results.Redirect(authorizeUrl);
});

// ==================== 4. Code 重复使用测试 ====================
app.MapGet("/code-reuse-test", async (HttpContext ctx) =>
{
    var savedCode = ctx.Session.GetString("last_auth_code");
    if (string.IsNullOrEmpty(savedCode))
    {
        return Results.Content(ResultPage
            .Replace("{title}", "Code 重复使用测试")
            .Replace("{headingColor}", "#e6a23c")
            .Replace("{content}", """
                <h2 style="color:#e6a23c;">Code 重复使用测试</h2>
                <p>请先通过 <a href="/login">标准登录</a> 完成一次 OAuth 流程，</p>
                <p>然后再回到此页面测试 code 能否重复使用。</p>
                <p>当前没有已缓存的 authorization code。</p>
            """), "text/html; charset=utf-8");
    }

    var httpClient = httpClientFactory;
    var codeVerifier = ctx.Session.GetString("code_verifier");

    var tokenParams = new List<KeyValuePair<string, string>>
    {
        new("grant_type", "authorization_code"),
        new("code", savedCode),
        new("redirect_uri", redirectUri),
        new("client_id", clientId),
        new("client_secret", clientSecret)
    };

    if (!string.IsNullOrEmpty(codeVerifier))
    {
        tokenParams.Add(new("code_verifier", codeVerifier));
    }

    var tokenResponse = await httpClient.PostAsync($"{oauthBaseUrl}/connect/token",
        new FormUrlEncodedContent(tokenParams));

    var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

    var content = !tokenResponse.IsSuccessStatusCode
        ? $"""
           <h2 style="color:#67c23a;">正确！服务端拒绝了重复使用的 code</h2>
           <p>状态码: {(int)tokenResponse.StatusCode}</p>
           <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
           """
        : $"""
           <h2 style="color:#f56c6c;">注意！code 重复使用成功</h2>
           <p>这表明服务端未正确校验 code 是否已使用</p>
           <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
           """;

    return Results.Content(ResultPage
        .Replace("{title}", "Code 重复使用测试")
        .Replace("{headingColor}", "#333")
        .Replace("{content}", content), "text/html; charset=utf-8");
});

// ==================== 5. 无效 client_secret ====================
app.MapGet("/invalid-secret", async (HttpContext ctx) =>
{
    var codeVerifier = GenerateCodeVerifier();
    var codeChallenge = GenerateCodeChallenge(codeVerifier);

    ctx.Session.SetString("code_verifier", codeVerifier);
    ctx.Session.SetString("use_pkce", "true");
    ctx.Session.SetString("invalid_secret_mode", "true");

    var state = Guid.NewGuid().ToString("N");
    ctx.Session.SetString("oauth_state", state);

    var authorizeUrl = $"{oauthBaseUrl}/connect/authorize" +
        $"?response_type=code" +
        $"&client_id={HttpUtility.UrlEncode(clientId)}" +
        $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
        $"&scope=openid profile email" +
        $"&state={state}" +
        $"&code_challenge={codeChallenge}" +
        $"&code_challenge_method=S256";

    return Results.Redirect(authorizeUrl);
});

// ==================== 6. 无效 Token 调用 UserInfo ====================
app.MapGet("/test-invalid-token", async (HttpContext ctx) =>
{
    var httpClient = httpClientFactory;

    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiZXhwIjo5OTk5OTk5OTk5fQ.fake-signature");

    var userInfoResponse = await httpClient.GetAsync($"{oauthBaseUrl}/connect/userinfo");
    var userInfoBody = await userInfoResponse.Content.ReadAsStringAsync();

    var content = $"""
        <h2 style="color:#e6a23c;">无效 Token 测试结果</h2>
        <p>使用伪造的 Bearer token 调用 UserInfo 接口</p>
        <div class="section">
            <h3>响应信息</h3>
            <p>状态码: <strong>{(int)userInfoResponse.StatusCode}</strong></p>
            <pre>{HttpUtility.HtmlEncode(userInfoBody)}</pre>
        </div>
        <p>期望结果: 401 Unauthorized</p>
        """;

    return Results.Content(ResultPage
        .Replace("{title}", "无效 Token 测试")
        .Replace("{headingColor}", "#333")
        .Replace("{content}", content), "text/html; charset=utf-8");
});

// ==================== Callback（统一回调处理） ====================
app.MapGet("/callback", async (HttpContext ctx) =>
{
    var code = ctx.Request.Query["code"].FirstOrDefault();
    var state = ctx.Request.Query["state"].FirstOrDefault();
    var error = ctx.Request.Query["error"].FirstOrDefault();

    if (!string.IsNullOrEmpty(error))
    {
        var errorContent = $"""
            <h2 style="color:#f56c6c;">授权失败</h2>
            <p>用户拒绝了授权或授权过程中发生错误</p>
            <div class="section">
                <h3>错误信息</h3>
                <p>error: <code>{HttpUtility.HtmlEncode(error)}</code></p>
            </div>
            <p>结论: 当用户拒绝授权时，服务端返回错误参数到 redirect_uri。</p>
            """;

        return Results.Content(ResultPage
            .Replace("{title}", "授权失败")
            .Replace("{headingColor}", "#f56c6c")
            .Replace("{content}", errorContent), "text/html; charset=utf-8");
    }

    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
    {
        var missingContent = $"""
            <h2 style="color:#f56c6c;">错误</h2>
            <p>缺少必要的参数</p>
            <p>code: {HttpUtility.HtmlEncode(code ?? "null")}</p>
            <p>state: {HttpUtility.HtmlEncode(state ?? "null")}</p>
            """;

        return Results.Content(ResultPage
            .Replace("{title}", "参数错误")
            .Replace("{headingColor}", "#f56c6c")
            .Replace("{content}", missingContent), "text/html; charset=utf-8");
    }

    var savedState = ctx.Session.GetString("oauth_state");
    if (string.IsNullOrEmpty(savedState) || savedState != state)
    {
        var csrfContent = $"""
            <h2 style="color:#f56c6c;">CSRF 攻击检测</h2>
            <p>state 参数不匹配，可能遭受 CSRF 攻击</p>
            <div class="section">
                <h3>详细信息</h3>
                <p>收到 state: {HttpUtility.HtmlEncode(state)}</p>
                <p>期望 state: {HttpUtility.HtmlEncode(savedState ?? "null")}</p>
            </div>
            """;

        return Results.Content(ResultPage
            .Replace("{title}", "CSRF 检测")
            .Replace("{headingColor}", "#f56c6c")
            .Replace("{content}", csrfContent), "text/html; charset=utf-8");
    }
    ctx.Session.Remove("oauth_state");

    // 保存 code 供重复使用测试
    ctx.Session.SetString("last_auth_code", code);

    var usePkce = ctx.Session.GetString("use_pkce") == "true";
    var codeVerifier = usePkce ? ctx.Session.GetString("code_verifier") : null;

    var invalidSecretMode = ctx.Session.GetString("invalid_secret_mode") == "true";
    ctx.Session.Remove("invalid_secret_mode");

    var effectiveClientSecret = invalidSecretMode ? "wrong-client-secret-for-testing" : clientSecret;

    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    var tokenParams = new List<KeyValuePair<string, string>>
    {
        new("grant_type", "authorization_code"),
        new("code", code),
        new("redirect_uri", redirectUri),
        new("client_id", clientId),
        new("client_secret", effectiveClientSecret)
    };

    if (!string.IsNullOrEmpty(codeVerifier))
    {
        tokenParams.Add(new("code_verifier", codeVerifier));
    }

    var tokenResponse = await httpClient.PostAsync($"{oauthBaseUrl}/connect/token",
        new FormUrlEncodedContent(tokenParams));

    var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

    if (invalidSecretMode)
    {
        var invalidSecretContent = !tokenResponse.IsSuccessStatusCode
            ? $"""
               <h2 style="color:#67c23a;">正确！服务端拒绝了无效的 client_secret</h2>
               <p>状态码: {(int)tokenResponse.StatusCode}</p>
               <div class="section">
                   <h3>使用的凭证</h3>
                   <p>client_id: {HttpUtility.HtmlEncode(clientId)}</p>
                   <p>client_secret: <strong>wrong-client-secret-for-testing</strong></p>
               </div>
               <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
               """
            : $"""
               <h2 style="color:#f56c6c;">注意！无效 secret 竟被接受了</h2>
               <p>这表明服务端未正确校验 client_secret</p>
               <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
               """;

        return Results.Content(ResultPage
            .Replace("{title}", "无效 Secret 测试")
            .Replace("{headingColor}", "#333")
            .Replace("{content}", invalidSecretContent), "text/html; charset=utf-8");
    }

    if (!tokenResponse.IsSuccessStatusCode)
    {
        var failContent = $"""
            <h2 style="color:#f56c6c;">获取 Token 失败</h2>
            <p>状态码: {(int)tokenResponse.StatusCode}</p>
            <p>PKCE: {(usePkce ? "是" : "否")}</p>
            <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
            """;

        return Results.Content(ResultPage
            .Replace("{title}", "Token 失败")
            .Replace("{headingColor}", "#f56c6c")
            .Replace("{content}", failContent), "text/html; charset=utf-8");
    }

    using var tokenDoc = JsonDocument.Parse(tokenBody);
    var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();
    var refreshToken = tokenDoc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var userInfoResponse = await httpClient.GetAsync($"{oauthBaseUrl}/connect/userinfo");
    var userInfoBody = userInfoResponse.IsSuccessStatusCode
        ? await userInfoResponse.Content.ReadAsStringAsync()
        : "获取用户信息失败";

    var formattedToken = JsonSerializer.Serialize(
        JsonDocument.Parse(tokenBody).RootElement,
        new JsonSerializerOptions { WriteIndented = true }
    );

    string formattedUserInfo;
    if (userInfoResponse.IsSuccessStatusCode)
    {
        formattedUserInfo = JsonSerializer.Serialize(
            JsonDocument.Parse(userInfoBody).RootElement,
            new JsonSerializerOptions { WriteIndented = true }
        );
    }
    else
    {
        formattedUserInfo = userInfoBody;
    }

    var refreshTokenDisplay = refreshToken != null
        ? refreshToken[..Math.Min(50, refreshToken.Length)] + "..."
        : "无";

    var callbackHtml = $"""
        <h2 style="color:#67c23a;">OAuth 登录成功</h2>

        <div class="section">
            <h3>基本信息</h3>
            <p>模式: {(usePkce ? "Authorization Code + PKCE" : "Authorization Code（无 PKCE）")}</p>
            <p>Authorization Code: <code>{HttpUtility.HtmlEncode(code)}</code></p>
            <p>此 code 已被服务端标记为已使用，可前往 <a href="/code-reuse-test">Code 重复使用测试</a> 验证</p>
        </div>

        <div class="section">
            <h3>Tokens</h3>
            <pre>{HttpUtility.HtmlEncode(formattedToken)}</pre>
        </div>

        <div class="section">
            <h3>用户信息</h3>
            <pre>{HttpUtility.HtmlEncode(formattedUserInfo)}</pre>
        </div>

        <div class="section">
            <h3>快捷操作</h3>
            <p>Refresh Token: <code>{refreshTokenDisplay}</code></p>
            <a class="btn" href="/refresh?token={HttpUtility.UrlEncode(refreshToken ?? "")}">刷新 Token</a>
            <a class="btn btn-warn" href="/code-reuse-test">测试 Code 重复使用</a>
        </div>
        """;

    return Results.Content(ResultPage
        .Replace("{title}", "登录成功")
        .Replace("{headingColor}", "#67c23a")
        .Replace("{content}", callbackHtml), "text/html; charset=utf-8");
});

// ==================== 刷新 Token ====================
app.MapGet("/refresh", async (HttpContext ctx) =>
{
    var refreshTokenValue = ctx.Request.Query["token"].FirstOrDefault();

    if (string.IsNullOrEmpty(refreshTokenValue))
    {
        return Results.Content(ResultPage
            .Replace("{title}", "刷新 Token")
            .Replace("{headingColor}", "#f56c6c")
            .Replace("{content}", """
                <h2 style="color:#f56c6c;">错误</h2>
                <p>缺少 refresh_token 参数</p>
                """), "text/html; charset=utf-8");
    }

    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    // 保存旧 refresh_token 供旋转测试
    ctx.Session.SetString("last_refresh_token", refreshTokenValue);

    var tokenParams = new List<KeyValuePair<string, string>>
    {
        new("grant_type", "refresh_token"),
        new("refresh_token", refreshTokenValue),
        new("client_id", clientId),
        new("client_secret", clientSecret)
    };

    var tokenResponse = await httpClient.PostAsync($"{oauthBaseUrl}/connect/token",
        new FormUrlEncodedContent(tokenParams));

    var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

    if (!tokenResponse.IsSuccessStatusCode)
    {
        var failContent = $"""
            <h2 style="color:#f56c6c;">刷新失败</h2>
            <p>可能原因: refresh_token 已过期、已被撤销或无效</p>
            <p>状态码: {(int)tokenResponse.StatusCode}</p>
            <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
            """;

        return Results.Content(ResultPage
            .Replace("{title}", "刷新失败")
            .Replace("{headingColor}", "#f56c6c")
            .Replace("{content}", failContent), "text/html; charset=utf-8");
    }

    var formatted = JsonSerializer.Serialize(
        JsonDocument.Parse(tokenBody).RootElement,
        new JsonSerializerOptions { WriteIndented = true }
    );

    var successContent = $"""
        <h2 style="color:#67c23a;">Token 刷新成功</h2>
        <p>旧的 refresh_token 已被撤销，新的 refresh_token 已颁发</p>
        <pre>{HttpUtility.HtmlEncode(formatted)}</pre>
        """;

    return Results.Content(ResultPage
        .Replace("{title}", "刷新成功")
        .Replace("{headingColor}", "#67c23a")
        .Replace("{content}", successContent), "text/html; charset=utf-8");
});

// ==================== 7. 错误 redirect_uri ====================
app.MapGet("/wrong-redirect-uri", async (HttpContext ctx) =>
{
    var savedCode = ctx.Session.GetString("last_auth_code");
    if (string.IsNullOrEmpty(savedCode))
    {
        return Results.Content(ResultPage
            .Replace("{title}", "错误 redirect_uri 测试")
            .Replace("{headingColor}", "#e6a23c")
            .Replace("{content}", """
                <h2 style="color:#e6a23c;">错误 redirect_uri 测试</h2>
                <p>请先通过 <a href="/login">标准登录（PKCE）</a> 完成一次 OAuth 流程，</p>
                <p>然后再回到此页面测试用错误的 redirect_uri 换取 Token。</p>
                <p>当前没有已缓存的 authorization code。</p>
            """), "text/html; charset=utf-8");
    }

    var codeVerifier = ctx.Session.GetString("code_verifier");

    var tokenParams = new List<KeyValuePair<string, string>>
    {
        new("grant_type", "authorization_code"),
        new("code", savedCode),
        new("redirect_uri", "http://localhost:9999/fake-callback"),
        new("client_id", clientId),
        new("client_secret", clientSecret)
    };

    if (!string.IsNullOrEmpty(codeVerifier))
    {
        tokenParams.Add(new("code_verifier", codeVerifier));
    }

    var httpClient = httpClientFactory;
    var tokenResponse = await httpClient.PostAsync($"{oauthBaseUrl}/connect/token",
        new FormUrlEncodedContent(tokenParams));
    var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

    var content = !tokenResponse.IsSuccessStatusCode
        ? $"""
           <h2 style="color:#67c23a;">正确！服务端拒绝了错误的 redirect_uri</h2>
           <p>状态码: {(int)tokenResponse.StatusCode}</p>
           <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
           """
        : $"""
           <h2 style="color:#f56c6c;">注意！错误的 redirect_uri 竟通过了</h2>
           <p>这表明服务端未正确校验 redirect_uri</p>
           <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
           """;

    return Results.Content(ResultPage
        .Replace("{title}", "错误 redirect_uri 测试")
        .Replace("{headingColor}", "#333")
        .Replace("{content}", content), "text/html; charset=utf-8");
});

// ==================== 8. 错误 code_verifier ====================
app.MapGet("/wrong-code-verifier", async (HttpContext ctx) =>
{
    var savedCode = ctx.Session.GetString("last_auth_code");
    if (string.IsNullOrEmpty(savedCode))
    {
        return Results.Content(ResultPage
            .Replace("{title}", "错误 code_verifier 测试")
            .Replace("{headingColor}", "#e6a23c")
            .Replace("{content}", """
                <h2 style="color:#e6a23c;">错误 code_verifier 测试</h2>
                <p>请先通过 <a href="/login">标准登录（PKCE）</a> 完成一次 OAuth 流程，</p>
                <p>然后再回到此页面测试用错误的 code_verifier 换取 Token。</p>
                <p>当前没有已缓存的 authorization code。</p>
            """), "text/html; charset=utf-8");
    }

    var fakeCodeVerifier = GenerateCodeVerifier();

    var tokenParams = new List<KeyValuePair<string, string>>
    {
        new("grant_type", "authorization_code"),
        new("code", savedCode),
        new("redirect_uri", redirectUri),
        new("client_id", clientId),
        new("client_secret", clientSecret),
        new("code_verifier", fakeCodeVerifier)
    };

    var httpClient = httpClientFactory;
    var tokenResponse = await httpClient.PostAsync($"{oauthBaseUrl}/connect/token",
        new FormUrlEncodedContent(tokenParams));
    var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

    var content = !tokenResponse.IsSuccessStatusCode
        ? $"""
           <h2 style="color:#67c23a;">正确！服务端拒绝了错误的 code_verifier</h2>
           <p>状态码: {(int)tokenResponse.StatusCode}</p>
           <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
           """
        : $"""
           <h2 style="color:#f56c6c;">注意！错误的 code_verifier 竟通过了</h2>
           <p>这表明服务端未正确验证 PKCE code_verifier</p>
           <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
           """;

    return Results.Content(ResultPage
        .Replace("{title}", "错误 code_verifier 测试")
        .Replace("{headingColor}", "#333")
        .Replace("{content}", content), "text/html; charset=utf-8");
});

// ==================== 9. 跨客户端 Code ====================
app.MapGet("/cross-client-code", async (HttpContext ctx) =>
{
    var savedCode = ctx.Session.GetString("last_auth_code");
    if (string.IsNullOrEmpty(savedCode))
    {
        return Results.Content(ResultPage
            .Replace("{title}", "跨客户端 Code 测试")
            .Replace("{headingColor}", "#e6a23c")
            .Replace("{content}", """
                <h2 style="color:#e6a23c;">跨客户端 Code 测试</h2>
                <p>请先通过 <a href="/login">标准登录（PKCE）</a> 完成一次 OAuth 流程，</p>
                <p>然后再回到此页面测试用其他客户端的凭证使用本客户端拿到的 code。</p>
                <p>当前没有已缓存的 authorization code。</p>
                <p>需在 appsettings.json 中配置 OtherClientId / OtherClientSecret</p>
            """), "text/html; charset=utf-8");
    }

    var codeVerifier = ctx.Session.GetString("code_verifier");

    var tokenParams = new List<KeyValuePair<string, string>>
    {
        new("grant_type", "authorization_code"),
        new("code", savedCode),
        new("redirect_uri", redirectUri),
        new("client_id", otherClientId),
        new("client_secret", otherClientSecret)
    };

    if (!string.IsNullOrEmpty(codeVerifier))
    {
        tokenParams.Add(new("code_verifier", codeVerifier));
    }

    var httpClient = httpClientFactory;
    var tokenResponse = await httpClient.PostAsync($"{oauthBaseUrl}/connect/token",
        new FormUrlEncodedContent(tokenParams));
    var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

    var content = !tokenResponse.IsSuccessStatusCode
        ? $"""
           <h2 style="color:#67c23a;">正确！服务端拒绝了跨客户端使用 code</h2>
           <p>状态码: {(int)tokenResponse.StatusCode}</p>
           <p>用客户端 <code>{HttpUtility.HtmlEncode(otherClientId)}</code> 的凭证尝试使用</p>
           <p>本客户端 <code>{HttpUtility.HtmlEncode(clientId)}</code> 拿到的 code</p>
           <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
           """
        : $"""
           <h2 style="color:#f56c6c;">注意！跨客户端使用 code 竟通过了</h2>
           <p>这表明服务端未正确校验 code 所属的 client 身份</p>
           <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
           """;

    return Results.Content(ResultPage
        .Replace("{title}", "跨客户端 Code 测试")
        .Replace("{headingColor}", "#333")
        .Replace("{content}", content), "text/html; charset=utf-8");
});

// ==================== 10. Refresh Token 旋转测试 ====================
app.MapGet("/refresh-rotation-test", async (HttpContext ctx) =>
{
    var refreshTokenValue = ctx.Session.GetString("last_refresh_token");
    if (string.IsNullOrEmpty(refreshTokenValue))
    {
        var tempContent = $"""
            <h2 style="color:#e6a23c;">Refresh Token 旋转测试</h2>
            <p>请先通过 <a href="/refresh?token=dummy">刷新 Token</a> 完成一次刷新后，</p>
            <p>再使用旧的 refresh_token 访问此页面。</p>
            <p>说明：此页面需要先用标准登录获取 Token，再用 <strong>旧 refresh_token</strong> 测试。</p>
            """;

        return Results.Content(ResultPage
            .Replace("{title}", "Refresh Token 旋转测试")
            .Replace("{headingColor}", "#e6a23c")
            .Replace("{content}", tempContent), "text/html; charset=utf-8");
    }

    var tokenParams = new List<KeyValuePair<string, string>>
    {
        new("grant_type", "refresh_token"),
        new("refresh_token", refreshTokenValue),
        new("client_id", clientId),
        new("client_secret", clientSecret)
    };

    var httpClient = httpClientFactory;
    var tokenResponse = await httpClient.PostAsync($"{oauthBaseUrl}/connect/token",
        new FormUrlEncodedContent(tokenParams));
    var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

    var content = !tokenResponse.IsSuccessStatusCode
        ? $"""
           <h2 style="color:#67c23a;">正确！服务端拒绝了已吊销的旧 refresh_token</h2>
           <p>Refresh Token 旋转机制生效：使用旧 token 被拒绝</p>
           <p>状态码: {(int)tokenResponse.StatusCode}</p>
           <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
           """
        : $"""
           <h2 style="color:#f56c6c;">注意！旧的 refresh_token 仍然可用</h2>
           <p>这表明服务端未正确吊销已旋转的 refresh_token</p>
           <pre>{HttpUtility.HtmlEncode(tokenBody)}</pre>
           """;

    ctx.Session.Remove("last_refresh_token");

    return Results.Content(ResultPage
        .Replace("{title}", "Refresh Token 旋转测试")
        .Replace("{headingColor}", "#333")
        .Replace("{content}", content), "text/html; charset=utf-8");
});

app.Run($"http://{redirectUri.Replace("http://", "").TrimEnd('/')}");

// ==================== PKCE 工具 ====================
static string GenerateCodeVerifier()
{
    var bytes = new byte[32];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(bytes);
    return Convert.ToBase64String(bytes)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
}

static string GenerateCodeChallenge(string codeVerifier)
{
    var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
    return Convert.ToBase64String(bytes)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
}