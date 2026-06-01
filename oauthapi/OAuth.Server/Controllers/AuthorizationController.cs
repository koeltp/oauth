using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Abstractions;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Authorization;
using OAuth.Contracts.Common;
using System.Security.Claims;
using Taipi.Core.RQRS;

namespace OAuth.Server.Controllers;

[Route("connect")]
public class AuthorizationController : Controller
{
    private readonly IOAuthAuthorizationService _authorizationService;
    private readonly IClientService _clientService;
    private readonly ICurrentUserService _currentUserService;

    public AuthorizationController(
        IOAuthAuthorizationService authorizationService, 
        IClientService clientService,
        ICurrentUserService currentUserService)
    {
        _authorizationService = authorizationService;
        _clientService = clientService;
        _currentUserService = currentUserService;
    }

    [HttpGet("authorize")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<ResponseResult<AuthorizeResponse>> Authorize()
    {
        var clientId = Request.Query["client_id"].ToString();
        var scope = Request.Query["scope"].ToString();

        if (string.IsNullOrEmpty(clientId))
        {
            return ResponseResult<AuthorizeResponse>.BadRequest("缺少客户端标识");
        }

        var client = await _clientService.GetByClientIdAsync(clientId);
        if (client == null)
        {
            return ResponseResult<AuthorizeResponse>.BadRequest("无效的客户端");
        }

        var userIdValue = _currentUserService.GetUserId()?.ToString();

        return new ResponseResult<AuthorizeResponse>(new AuthorizeResponse
        {
            ClientId = client.ClientId,
            ClientName = client.Name,
            Scopes = scope,
            UserId = userIdValue
        });
    }

    [HttpPost("authorize/accept")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Accept()
    {
        var userId = _currentUserService.GetUserId();
        
        if (userId == null)
        {
            return BadRequest(ResponseResult<object>.BadRequest("无效的用户"));
        }

        var clientId = Request.Form["client_id"].Count > 0
            ? Request.Form["client_id"].ToString()
            : Request.Query["client_id"].ToString();
        var scope = Request.Form["scope"].Count > 0
            ? Request.Form["scope"].ToString()
            : Request.Query["scope"].ToString();
        var redirectUri = Request.Form["redirect_uri"].Count > 0
            ? Request.Form["redirect_uri"].ToString()
            : Request.Query["redirect_uri"].ToString();
        var codeChallenge = Request.Form["code_challenge"].Count > 0
            ? Request.Form["code_challenge"].ToString()
            : Request.Query["code_challenge"].ToString();
        var codeChallengeMethod = Request.Form["code_challenge_method"].Count > 0
            ? Request.Form["code_challenge_method"].ToString()
            : Request.Query["code_challenge_method"].ToString();

        if (string.IsNullOrEmpty(clientId))
        {
            return BadRequest(ResponseResult<object>.BadRequest("缺少客户端标识"));
        }

        var client = await _clientService.GetByClientIdAsync(clientId);
        if (client == null)
        {
            return BadRequest(ResponseResult<object>.BadRequest("无效的客户端"));
        }

        await _authorizationService.CreateAsync(userId.Value, client.Id, scope, redirectUri, codeChallenge, codeChallengeMethod);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, User.FindFirstValue(ClaimTypes.Name) ?? ""),
            new(ClaimTypes.Email, User.FindFirstValue(ClaimTypes.Email) ?? "")
        };

        var identity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var scopes = scope?.Split(' ') ?? Array.Empty<string>();
        principal.SetScopes(scopes);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("authorize/deny")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult Deny()
    {
        return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}