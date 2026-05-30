using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Common;
using OAuth.Contracts.User;
using OAuth.Domain.Entities;

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
    public async Task<ApiResponse<UserInfoResponse>> GetUserInfo()
    {
        var id = _currentUserService.GetUserId();
        if (id == null)
        {
            return new ApiResponse<UserInfoResponse> { Code = 401, Message = "未授权" };
        }

        var user = await _userService.GetByIdAsync(id.Value);
        if (user == null)
        {
            return new ApiResponse<UserInfoResponse> { Code = 404, Message = "用户未找到" };
        }

        return new ApiResponse<UserInfoResponse>
        {
            Data = new UserInfoResponse
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
            }
        };
    }

    [HttpGet("authorizations")]
    public async Task<ApiResponse<IEnumerable<AuthorizationResponse>>> GetAuthorizations()
    {
        var id = _currentUserService.GetUserId();
        if (id == null)
        {
            return new ApiResponse<IEnumerable<AuthorizationResponse>> { Code = 401, Message = "未授权" };
        }

        var authorizations = await _authorizationService.GetByUserIdAsync(id.Value);
        return new ApiResponse<IEnumerable<AuthorizationResponse>>
        {
            Data = authorizations.Select(a => new AuthorizationResponse
            {
                Id = a.Id,
                ClientId = a.ClientId,
                ClientName = a.Client?.Name,
                Scope = a.Scope,
                CreatedAt = a.CreatedAt
            })
        };
    }

    [HttpDelete("authorizations/{id}")]
    public async Task<ApiResponse<AuthorizationResponse>> RevokeAuthorization(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return new ApiResponse<AuthorizationResponse> { Code = 401, Message = "未授权" };
        }

        var authorization = await _authorizationService.GetByIdAsync(id);
        if (authorization == null)
        {
            return new ApiResponse<AuthorizationResponse> { Code = 404, Message = "授权未找到" };
        }

        if (authorization.UserId != userId.Value)
        {
            return new ApiResponse<AuthorizationResponse> { Code = 403, Message = "禁止操作" };
        }

        var response = new AuthorizationResponse
        {
            Id = authorization.Id,
            ClientId = authorization.ClientId,
            ClientName = authorization.Client?.Name,
            Scope = authorization.Scope,
            CreatedAt = authorization.CreatedAt
        };

        await _authorizationService.DeleteAsync(id);
        return new ApiResponse<AuthorizationResponse> { Data = response, Message = "授权已撤销" };
    }

    [HttpGet("external/accounts")]
    public async Task<ApiResponse<IEnumerable<BoundAccountResponse>>> GetExternalAccounts()
    {
        var id = _currentUserService.GetUserId();
        if (id == null)
        {
            return new ApiResponse<IEnumerable<BoundAccountResponse>> { Code = 401, Message = "未授权" };
        }

        var accounts = await _externalAccountService.GetByUserIdAsync(id.Value);
        return new ApiResponse<IEnumerable<BoundAccountResponse>>
        {
            Data = accounts.Select(a => new BoundAccountResponse
            {
                Id = a.Id,
                Provider = a.Provider.ToString(),
                CreatedAt = a.CreatedAt
            })
        };
    }

    [HttpDelete("external/accounts/{id}")]
    public async Task<ApiResponse<BoundAccountResponse>> UnbindExternalAccount(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return new ApiResponse<BoundAccountResponse> { Code = 401, Message = "未授权" };
        }

        var account = await _externalAccountService.GetByIdAsync(id);
        if (account == null)
        {
            return new ApiResponse<BoundAccountResponse> { Code = 404, Message = "账号未找到" };
        }

        if (account.UserId != userId.Value)
        {
            return new ApiResponse<BoundAccountResponse> { Code = 403, Message = "禁止操作" };
        }

        var response = new BoundAccountResponse
        {
            Id = account.Id,
            Provider = account.Provider.ToString(),
            CreatedAt = account.CreatedAt
        };

        await _externalAccountService.DeleteAsync(id);
        return new ApiResponse<BoundAccountResponse> { Data = response, Message = "已解绑" };
    }
}