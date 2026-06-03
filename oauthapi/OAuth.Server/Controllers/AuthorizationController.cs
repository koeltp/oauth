using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Common;
using Taipi.Core.RQRS;

namespace OAuth.Server.Controllers;

[Route("connect")]
public class AuthorizationController : Controller
{
    private readonly IOAuthAuthorizationService _authorizationService;
    private readonly IClientService _clientService;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _configuration;

    public AuthorizationController(
        IOAuthAuthorizationService authorizationService, 
        IClientService clientService,
        IUserService userService,
        ICurrentUserService currentUserService,
        IConfiguration configuration)
    {
        _authorizationService = authorizationService;
        _clientService = clientService;
        _userService = userService;
        _currentUserService = currentUserService;
        _configuration = configuration;
    }

    [HttpGet("authorize")]
    public IActionResult RedirectToAuthorizationPage()
    {
        var frontendBaseUrl = _configuration.GetValue<string>("FrontendBaseUrl") 
            ?? "http://localhost:3000";
        var redirectUrl = $"{frontendBaseUrl}/authorize{Request.QueryString}";
        return Redirect(redirectUrl);
    }

    [HttpPost("authorize/accept")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null)
        {
            return BadRequest(ResponseResult<object>.BadRequest("用户不存在"));
        }

        var authorization = await _authorizationService.CreateAsync(userId.Value, client.Id, scope, redirectUri, codeChallenge, codeChallengeMethod);

        return Ok(new { code = authorization.Code });
    }

    [HttpPost("authorize/deny")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult Deny()
    {
        return Ok(new { error = "access_denied" });
    }
}