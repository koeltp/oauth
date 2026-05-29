using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OAuth.Application.Interfaces;
using OAuth.Contracts.ExternalAccount;
using OAuth.Domain.Entities;
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

    public ExternalAccountController(
        IExternalAccountService externalAccountService,
        IUserService userService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _externalAccountService = externalAccountService;
        _userService = userService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet("wechat/authorize")]
    public IActionResult WechatAuthorize([FromQuery] string? redirect_uri)
    {
        var appId = _configuration["Wechat:AppId"] ?? "your-wechat-appid";
        var callbackUrl = _configuration["Wechat:CallbackUrl"] ?? "https://localhost:5001/api/external/wechat/callback";
        var state = Guid.NewGuid().ToString("N");

        Response.Cookies.Append("oauth_state", state, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddMinutes(10)
        });

        var url = $"https://open.weixin.qq.com/connect/qrconnect?appid={appId}&redirect_uri={Uri.EscapeDataString(redirect_uri ?? callbackUrl)}&response_type=code&scope=snsapi_login&state={state}#wechat_redirect";

        return Redirect(url);
    }

    [AllowAnonymous]
    [HttpGet("github/authorize")]
    public IActionResult GithubAuthorize([FromQuery] string? redirect_uri)
    {
        var clientId = _configuration["GitHub:ClientId"] ?? "your-github-client-id";
        var callbackUrl = _configuration["GitHub:CallbackUrl"] ?? "https://localhost:5001/api/external/github/callback";
        var state = Guid.NewGuid().ToString("N");

        Response.Cookies.Append("oauth_state", state, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddMinutes(10)
        });

        var url = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirect_uri ?? callbackUrl)}&scope=user:email&state={state}";

        return Redirect(url);
    }

    [AllowAnonymous]
    [HttpGet("github/callback")]
    public async Task<IActionResult> GithubCallback([FromQuery] string code, [FromQuery] string state)
    {
        var storedState = Request.Cookies["oauth_state"];
        if (state != storedState)
        {
            return BadRequest(new { message = "Invalid state" });
        }

        Response.Cookies.Delete("oauth_state");

        var clientId = _configuration["GitHub:ClientId"] ?? "your-github-client-id";
        var clientSecret = _configuration["GitHub:ClientSecret"] ?? "your-github-client-secret";
        var callbackUrl = _configuration["GitHub:CallbackUrl"] ?? "https://localhost:5001/api/external/github/callback";

        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            var tokenResponse = await httpClient.PostAsync("https://github.com/login/oauth/access_token",
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", callbackUrl),
                    new KeyValuePair<string, string>("state", state)
                }));

            tokenResponse.EnsureSuccessStatusCode();

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
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
                return BadRequest(new { message = "Failed to get access token" });
            }

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var userResponse = await httpClient.GetAsync("https://api.github.com/user");
            userResponse.EnsureSuccessStatusCode();

            var userContent = await userResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(userContent);
            var githubUserId = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;
            var email = doc.RootElement.GetProperty("login").GetString() ?? string.Empty;

            var existingAccount = await _externalAccountService.GetByProviderAsync(ExternalProvider.Github, githubUserId);
            if (existingAccount != null)
            {
                var user = await _userService.GetByIdAsync(existingAccount.UserId);
                return Ok(new
                {
                    bound = true,
                    user_id = existingAccount.UserId,
                    email = user?.Email
                });
            }

            return Ok(new
            {
                bound = false,
                provider = "github",
                provider_user_id = githubUserId,
                email = email
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"OAuth callback failed: {ex.Message}" });
        }
    }

    [HttpPost("bind")]
    [Authorize]
    public async Task<IActionResult> Bind([FromBody] BindExternalAccountRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        await _externalAccountService.BindAsync(id, request.Provider, request.ProviderUserId);

        return Ok(new { message = "External account bound successfully" });
    }

    [HttpPost("unbind")]
    [Authorize]
    public async Task<IActionResult> Unbind([FromBody] UnbindExternalAccountRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        await _externalAccountService.UnbindAsync(id, request.Provider);

        return Ok(new { message = "External account unbound successfully" });
    }

    [HttpGet("bound-accounts")]
    [Authorize]
    public async Task<IActionResult> GetBoundAccounts()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await _userService.GetByIdAsync(id);
        var accounts = user?.ExternalAccounts.Select(e => new
        {
            e.Id,
            provider = e.Provider.ToString(),
            e.CreatedAt
        });

        return Ok(accounts);
    }
}
