using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Domain.Entities;
using System.Security.Claims;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/user")]
[ApiVersion("1.0")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IOAuthAuthorizationService _authorizationService;
    private readonly IUserExternalAccountService _externalAccountService;

    public UserController(
        IUserService userService,
        IOAuthAuthorizationService authorizationService,
        IUserExternalAccountService externalAccountService)
    {
        _userService = userService;
        _authorizationService = authorizationService;
        _externalAccountService = externalAccountService;
    }

    [HttpGet("info")]
    public async Task<IActionResult> GetUserInfo()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            phone = user.Phone,
            email_verified = user.EmailVerified,
            phone_verified = user.PhoneVerified,
            two_factor_enabled = user.TwoFactorEnabled,
            status = user.Status,
            created_at = user.CreatedAt
        });
    }

    [HttpGet("authorizations")]
    public async Task<IActionResult> GetAuthorizations()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var authorizations = await _authorizationService.GetByUserIdAsync(id);
        return Ok(authorizations.Select(a => new
        {
            id = a.Id,
            client_id = a.ClientId,
            client_name = a.Client?.Name,
            scope = a.Scope,
            created_at = a.CreatedAt
        }));
    }

    [HttpDelete("authorizations/{id}")]
    public async Task<IActionResult> RevokeAuthorization(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            return Unauthorized();
        }

        var authorization = await _authorizationService.GetByIdAsync(id);
        if (authorization == null)
        {
            return NotFound();
        }

        if (authorization.UserId != userIdGuid)
        {
            return Forbid();
        }

        await _authorizationService.DeleteAsync(id);
        return Ok(new { message = "授权已撤销" });
    }

    [HttpGet("external/accounts")]
    public async Task<IActionResult> GetExternalAccounts()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var accounts = await _externalAccountService.GetByUserIdAsync(id);
        return Ok(accounts.Select(a => new
        {
            id = a.Id,
            provider = a.Provider,
            provider_user_id = a.ProviderUserId,
            created_at = a.CreatedAt
        }));
    }

    [HttpDelete("external/accounts/{id}")]
    public async Task<IActionResult> UnbindExternalAccount(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            return Unauthorized();
        }

        var account = await _externalAccountService.GetByIdAsync(id);
        if (account == null)
        {
            return NotFound();
        }

        if (account.UserId != userIdGuid)
        {
            return Forbid();
        }

        await _externalAccountService.DeleteAsync(id);
        return Ok(new { message = "已解绑" });
    }
}
