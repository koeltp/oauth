using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Common;
using OAuth.Contracts.User;
using Taipi.Core.RQRS;

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
    private readonly ICurrentUserService _currentUserService;

    public UserController(
        IUserService userService,
        IOAuthAuthorizationService authorizationService,
        IUserExternalAccountService externalAccountService,
        ICurrentUserService currentUserService)
    {
        _userService = userService;
        _authorizationService = authorizationService;
        _externalAccountService = externalAccountService;
        _currentUserService = currentUserService;
    }

    [HttpGet("info")]
    public async Task<ResponseResult<UserInfoResponse>> GetUserInfo()
    {
        var id = _currentUserService.GetUserId();
        if (id == null)
        {
            return ResponseResult<UserInfoResponse>.Unauthorized("未授权");
        }

        var user = await _userService.GetByIdAsync(id.Value);
        if (user == null)
        {
            return ResponseResult<UserInfoResponse>.NotFound("用户未找到");
        }

        return new ResponseResult<UserInfoResponse>(new UserInfoResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            EmailVerified = user.EmailVerified,
            PhoneVerified = user.PhoneVerified,
            TwoFactorEnabled = user.TwoFactorEnabled,
            Status = user.Status.ToString(),
            CreatedAt = user.CreatedAt
        });
    }

    [HttpGet("authorizations")]
    public async Task<ResponseResult<IEnumerable<AuthorizationResponse>>> GetAuthorizations()
    {
        var id = _currentUserService.GetUserId();
        if (id == null)
        {
            return ResponseResult<IEnumerable<AuthorizationResponse>>.Unauthorized("未授权");
        }

        var authorizations = await _authorizationService.GetByUserIdAsync(id.Value);
        return new ResponseResult<IEnumerable<AuthorizationResponse>>(authorizations.Select(a => new AuthorizationResponse
        {
            Id = a.Id,
            ClientId = a.ClientId,
            ClientName = a.Client?.Name,
            Logo = a.Client?.Logo,
            Scope = a.Scope,
            CreatedAt = a.CreatedAt
        }));
    }

    [HttpDelete("authorizations/{id}")]
    public async Task<ResponseResult<AuthorizationResponse>> RevokeAuthorization(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return ResponseResult<AuthorizationResponse>.Unauthorized("未授权");
        }

        var authorization = await _authorizationService.GetByIdAsync(id);
        if (authorization == null)
        {
            return ResponseResult<AuthorizationResponse>.NotFound("授权未找到");
        }

        if (authorization.UserId != userId.Value)
        {
            return ResponseResult<AuthorizationResponse>.Forbidden("禁止操作");
        }

        var response = new AuthorizationResponse
        {
            Id = authorization.Id,
            ClientId = authorization.ClientId,
            ClientName = authorization.Client?.Name,
            Logo = authorization.Client?.Logo,
            Scope = authorization.Scope,
            CreatedAt = authorization.CreatedAt
        };

        await _authorizationService.DeleteAsync(id);
        return new ResponseResult<AuthorizationResponse>(response) { Message = "授权已撤销" };
    }

    [HttpGet("external/accounts")]
    public async Task<ResponseResult<IEnumerable<BoundAccountResponse>>> GetExternalAccounts()
    {
        var id = _currentUserService.GetUserId();
        if (id == null)
        {
            return ResponseResult<IEnumerable<BoundAccountResponse>>.Unauthorized("未授权");
        }

        var accounts = await _externalAccountService.GetByUserIdAsync(id.Value);
        return new ResponseResult<IEnumerable<BoundAccountResponse>>(accounts.Select(a => new BoundAccountResponse
        {
            Id = a.Id,
            Provider = a.Provider.ToString(),
            CreatedAt = a.CreatedAt
        }));
    }

    [HttpDelete("external/accounts/{id}")]
    public async Task<ResponseResult<BoundAccountResponse>> UnbindExternalAccount(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return ResponseResult<BoundAccountResponse>.Unauthorized("未授权");
        }

        var account = await _externalAccountService.GetByIdAsync(id);
        if (account == null)
        {
            return ResponseResult<BoundAccountResponse>.NotFound("账号未找到");
        }

        if (account.UserId != userId.Value)
        {
            return ResponseResult<BoundAccountResponse>.Forbidden("禁止操作");
        }

        var response = new BoundAccountResponse
        {
            Id = account.Id,
            Provider = account.Provider.ToString(),
            CreatedAt = account.CreatedAt
        };

        await _externalAccountService.DeleteAsync(id);
        return new ResponseResult<BoundAccountResponse>(response) { Message = "已解绑" };
    }
}