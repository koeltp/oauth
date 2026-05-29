using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Abstractions;
using OAuth.Application.Interfaces;
using System.Security.Claims;

namespace OAuth.Server.Controllers;

[Route("connect")]
public class AuthorizationController : Controller
{
    private readonly IOAuthAuthorizationService _authorizationService;
    private readonly IClientService _clientService;

    public AuthorizationController(IOAuthAuthorizationService authorizationService, IClientService clientService)
    {
        _authorizationService = authorizationService;
        _clientService = clientService;
    }

    [HttpGet("authorize")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult Authorize()
    {
        return View("Authorize");
    }

    [HttpPost("authorize/accept")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Accept()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                          ?? User.FindFirstValue("sub");
        
        if (string.IsNullOrEmpty(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
        {
            return BadRequest(new { error = "invalid_user" });
        }
        
        var clientId = Request.Query["client_id"].ToString();
        var scope = Request.Query["scope"].ToString();

        var client = await _clientService.GetByClientIdAsync(clientId);
        if (client == null)
        {
            return BadRequest(new { error = "invalid_client" });
        }

        await _authorizationService.CreateAsync(userId, client.Id, scope);

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
