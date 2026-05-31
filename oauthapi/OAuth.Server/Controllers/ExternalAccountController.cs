using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Common;
using OAuth.Contracts.ExternalAccount;
using OAuth.Contracts.User;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Options;
using System.Text.Json;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/external")]
[ApiVersion("1.0")]
public class ExternalAccountController : ControllerBase
{
    private readonly IExternalAccountService _externalAccountService;
    private readonly IUserService _userService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJwtService _jwtService;
    private readonly TokenOptions _tokenOptions;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ExternalAccountController> _logger;

    public ExternalAccountController(
        IExternalAccountService externalAccountService,
        IUserService userService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ICurrentUserService currentUserService,
        IJwtService jwtService,
        IOptions<TokenOptions> tokenOptions,
        IDistributedCache cache,
        ILogger<ExternalAccountController> logger)
    {
        _externalAccountService = externalAccountService;
        _userService = userService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _currentUserService = currentUserService;
        _jwtService = jwtService;
        _tokenOptions = tokenOptions.Value;
        _cache = cache;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("wechat/authorize")]
    public async Task<IActionResult> WechatAuthorize([FromQuery] string? redirect_uri)
    {
        var appId = _configuration["Wechat:AppId"] ?? "your-wechat-appid";
        var callbackUrl = _configuration["Wechat:CallbackUrl"] ?? "https://localhost:5001/api/external/wechat/callback";
        var state = Guid.NewGuid().ToString("N");

        var cacheKey = $"oauth_state:{state}";
        await _cache.SetStringAsync(cacheKey, redirect_uri ?? "", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        var url = $"https://open.weixin.qq.com/connect/qrconnect?appid={appId}&redirect_uri={Uri.EscapeDataString(redirect_uri ?? callbackUrl)}&response_type=code&scope=snsapi_login&state={state}#wechat_redirect";

        return Redirect(url);
    }

    [AllowAnonymous]
    [HttpGet("github/authorize")]
    public async Task<IActionResult> GithubAuthorize([FromQuery] string? redirect_uri)
    {
        var clientId = _configuration["GitHub:ClientId"] ?? "your-github-client-id";
        var callbackUrl = _configuration["GitHub:CallbackUrl"] ?? "https://localhost:5001/api/1.0/external/github/callback";
        var state = Guid.NewGuid().ToString("N");

        _logger.LogInformation("GitHub authorize 开始 | redirect_uri={RedirectUri} | clientId={ClientId} | callbackUrl={CallbackUrl}",
            redirect_uri, clientId, callbackUrl);

        var cacheKey = $"oauth_state:{state}";
        await _cache.SetStringAsync(cacheKey, redirect_uri ?? "", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        _logger.LogInformation("GitHub authorize state 已缓存 | state={State} | cacheKey={CacheKey}", state, cacheKey);

        // 传给 GitHub 的 redirect_uri 始终是后端回调地址（需要在 GitHub OAuth App 中注册）
        var url = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(callbackUrl)}&scope=user:email&state={state}";

        _logger.LogInformation("GitHub authorize 跳转到 GitHub | url={Url}", url);
        return Redirect(url);
    }

    [AllowAnonymous]
    [HttpGet("github/callback")]
    public async Task<IActionResult> GithubCallback([FromQuery] string code, [FromQuery] string state)
    {
        _logger.LogInformation("GitHub callback 收到回调 | code={Code} | state={State}", code?.Length > 10 ? code[..10] + "..." : code, state);

        var cacheKey = $"oauth_state:{state}";
        var storedValue = await _cache.GetStringAsync(cacheKey);
        if (storedValue == null)
        {
            _logger.LogWarning("GitHub callback state 无效或已过期 | state={State}", state);
            return BadRequest(new { error = "invalid_state", message = "无效的状态参数" });
        }
        await _cache.RemoveAsync(cacheKey);

        _logger.LogInformation("GitHub callback state 验证通过");

        var frontendRedirect = string.IsNullOrEmpty(storedValue) ? null : storedValue;

        var clientId = _configuration["GitHub:ClientId"] ?? "your-github-client-id";
        var clientSecret = _configuration["GitHub:ClientSecret"] ?? "your-github-client-secret";
        var callbackUrl = _configuration["GitHub:CallbackUrl"] ?? "https://localhost:5001/api/1.0/external/github/callback";
        var frontendCallbackUrl = _configuration["GitHub:FrontendCallbackUrl"] ?? "https://localhost:5173/auth/github/callback";

        _logger.LogInformation("GitHub callback 配置加载完成 | clientId={ClientId} | callbackUrl={CallbackUrl} | frontendCallbackUrl={FrontendCallbackUrl}",
            clientId, callbackUrl, frontendCallbackUrl);

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("OAuth-Server/1.0");
            _logger.LogInformation("GitHub callback 正在请求 GitHub access_token...");

            var tokenResponse = await httpClient.PostAsync("https://github.com/login/oauth/access_token",
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", callbackUrl),
                    new KeyValuePair<string, string>("state", state)
                }));

            _logger.LogInformation("GitHub callback access_token 响应 | statusCode={(int)tokenResponse.StatusCode} | reason={tokenResponse.ReasonPhrase}",
                (int)tokenResponse.StatusCode, tokenResponse.ReasonPhrase);

            tokenResponse.EnsureSuccessStatusCode();

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("GitHub callback access_token 内容 | content={TokenContent}", tokenContent);

            var tokenParams = new Dictionary<string, string>();
            foreach (var param in tokenContent.Split('&'))
            {
                var parts = param.Split('=');
                if (parts.Length == 2)
                {
                    tokenParams[parts[0]] = parts[1];
                }
            }

            if (!tokenParams.TryGetValue("access_token", out var accessToken))
            {
                var errorDesc = tokenParams.GetValueOrDefault("error_description", "获取访问令牌失败");
                _logger.LogWarning("GitHub callback 获取 access_token 失败 | error={ErrorDesc}", errorDesc);
                return Redirect($"{frontendCallbackUrl}?error={Uri.EscapeDataString(errorDesc)}");
            }

            _logger.LogInformation("GitHub callback access_token 获取成功 | token={Token}", accessToken[..Math.Min(10, accessToken.Length)] + "...");

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            _logger.LogInformation("GitHub callback 正在请求 GitHub 用户信息...");

            var userResponse = await httpClient.GetAsync("https://api.github.com/user");
            _logger.LogInformation("GitHub callback 用户信息响应 | statusCode={(int)userResponse.StatusCode} | reason={userResponse.ReasonPhrase}",
                (int)userResponse.StatusCode, userResponse.ReasonPhrase);

            if (!userResponse.IsSuccessStatusCode)
            {
                var errorContent = await userResponse.Content.ReadAsStringAsync();
                _logger.LogError("GitHub callback 获取用户信息失败 | statusCode={(int)userResponse.StatusCode} | body={ErrorContent}",
                    (int)userResponse.StatusCode, errorContent);
                return Redirect($"{frontendCallbackUrl}?error={Uri.EscapeDataString($"获取 GitHub 用户信息失败 ({(int)userResponse.StatusCode})")}");
            }

            userResponse.EnsureSuccessStatusCode();

            var userContent = await userResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("GitHub callback 用户信息获取成功 | body={UserContent}", userContent);

            using var doc = JsonDocument.Parse(userContent);
            var githubUserId = doc.RootElement.GetProperty("id").GetInt64().ToString();
            var githubLogin = doc.RootElement.GetProperty("login").GetString() ?? string.Empty;

            _logger.LogInformation("GitHub callback 用户信息解析 | githubUserId={UserId} | githubLogin={Login}", githubUserId, githubLogin);

            // 获取 GitHub 邮箱（优先使用公开邮箱，否则尝试获取私有邮箱）
            var email = string.Empty;
            if (doc.RootElement.TryGetProperty("email", out var emailProp) && emailProp.ValueKind == JsonValueKind.String)
            {
                email = emailProp.GetString() ?? string.Empty;
                _logger.LogInformation("GitHub callback 从公开信息获取到邮箱 | email={Email}", email);
            }

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogInformation("GitHub callback 正在请求用户邮箱列表...");
                var emailResponse = await httpClient.GetAsync("https://api.github.com/user/emails");
                _logger.LogInformation("GitHub callback 邮箱列表响应 | statusCode={StatusCode}", (int)emailResponse.StatusCode);

                if (emailResponse.IsSuccessStatusCode)
                {
                    var emailContent = await emailResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("GitHub callback 邮箱列表内容 | content={EmailContent}", emailContent);
                    using var emailDocs = JsonDocument.Parse(emailContent);
                    foreach (var item in emailDocs.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("primary", out var primary) && primary.GetBoolean() &&
                            item.TryGetProperty("email", out var primaryEmail))
                        {
                            email = primaryEmail.GetString() ?? string.Empty;
                            _logger.LogInformation("GitHub callback 获取到主邮箱 | email={Email}", email);
                            break;
                        }
                    }
                }
                else
                {
                    var emailErrorContent = await emailResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("GitHub callback 获取邮箱列表失败 | statusCode={(int)emailResponse.StatusCode} | body={ErrorContent}",
                        (int)emailResponse.StatusCode, emailErrorContent);
                }
            }

            _logger.LogInformation("GitHub callback 最终邮箱 | email={Email}", email);

            var existingAccount = await _externalAccountService.GetByProviderAsync(ExternalProvider.Github, githubUserId);
            User user;

            if (existingAccount != null)
            {
                user = existingAccount.User;
                _logger.LogInformation("GitHub callback 找到已有绑定 | githubUserId={UserId} | systemUserId={SystemUserId}", githubUserId, user.Id);
            }
            else
            {
                _logger.LogInformation("GitHub callback 未找到绑定，准备创建新用户 | githubUserId={UserId} | githubLogin={Login}", githubUserId, githubLogin);

                // 自动注册新用户
                var username = GenerateUniqueUsername(githubLogin);
                user = new User
                {
                    Username = username,
                    Email = string.IsNullOrEmpty(email) ? $"{githubLogin}@github.user" : email,
                    EmailVerified = !string.IsNullOrEmpty(email)
                };

                _logger.LogInformation("GitHub callback 创建用户 | username={Username} | email={Email}", user.Username, user.Email);
                user = await _userService.CreateAsync(user, Guid.NewGuid().ToString("N"));
                _logger.LogInformation("GitHub callback 用户创建成功 | userId={UserId}", user.Id);

                await _externalAccountService.BindAsync(user.Id, ExternalProvider.Github, githubUserId, accessToken);
                _logger.LogInformation("GitHub callback 绑定成功 | userId={UserId} | githubUserId={GithubUserId}", user.Id, githubUserId);
            }

            var jwt = _jwtService.GenerateUserToken(user);
            var frontendUrl = !string.IsNullOrEmpty(frontendRedirect)
                ? frontendRedirect
                : frontendCallbackUrl;

            var redirectUrl = $"{frontendUrl}?access_token={Uri.EscapeDataString(jwt)}&user_id={user.Id}&username={Uri.EscapeDataString(user.Username)}&email={Uri.EscapeDataString(user.Email ?? string.Empty)}&expires_in={_jwtService.GetExpirationSeconds()}";

            _logger.LogInformation("GitHub callback 登录成功，重定向到前端 | frontendUrl={FrontendUrl}", frontendUrl);
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub callback 发生异常 | message={Message}", ex.Message);
            return Redirect($"{frontendCallbackUrl}?error={Uri.EscapeDataString($"OAuth 登录失败：{ex.Message}")}");
        }
    }

    private string GenerateUniqueUsername(string baseUsername)
    {
        var username = baseUsername;
        var counter = 0;
        while (true)
        {
            var existing = _userService.GetByUsernameAsync(username).GetAwaiter().GetResult();
            if (existing == null)
            {
                return username;
            }
            counter++;
            username = $"{baseUsername}{counter}";
        }
    }

    [HttpPost("bind")]
    [Authorize]
    public async Task<ApiResponse<BoundAccountResponse>> Bind([FromBody] BindExternalAccountRequest request)
    {
        var id = _currentUserService.GetUserId();
        if (id == null)
        {
            return new ApiResponse<BoundAccountResponse> { Code = 401, Message = "未授权" };
        }

        var account = await _externalAccountService.BindAsync(id.Value, request.Provider, request.ProviderUserId);

        return new ApiResponse<BoundAccountResponse>
        {
            Data = new BoundAccountResponse
            {
                Id = account.Id,
                Provider = account.Provider.ToString(),
                CreatedAt = account.CreatedAt
            },
            Message = "外部账号绑定成功"
        };
    }

    [HttpPost("unbind")]
    [Authorize]
    public async Task<ApiResponse<BoundAccountResponse>> Unbind([FromBody] UnbindExternalAccountRequest request)
    {
        var id = _currentUserService.GetUserId();
        if (id == null)
        {
            return new ApiResponse<BoundAccountResponse> { Code = 401, Message = "未授权" };
        }

        await _externalAccountService.UnbindAsync(id.Value, request.Provider);

        return new ApiResponse<BoundAccountResponse> { Message = "外部账号解绑成功" };
    }

    [HttpGet("bound-accounts")]
    [Authorize]
    public async Task<ApiResponse<IEnumerable<BoundAccountResponse>>> GetBoundAccounts()
    {
        var id = _currentUserService.GetUserId();
        if (id == null)
        {
            return new ApiResponse<IEnumerable<BoundAccountResponse>> { Code = 401, Message = "未授权" };
        }

        var user = await _userService.GetByIdAsync(id.Value);
        var accounts = user?.ExternalAccounts.Select(e => new BoundAccountResponse
        {
            Id = e.Id,
            Provider = e.Provider.ToString(),
            CreatedAt = e.CreatedAt
        });

        return new ApiResponse<IEnumerable<BoundAccountResponse>>
        {
            Data = accounts ?? Enumerable.Empty<BoundAccountResponse>()
        };
    }
}