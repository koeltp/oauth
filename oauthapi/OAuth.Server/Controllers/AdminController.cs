using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Admin;
using OAuth.Contracts.Auth;
using OAuth.Contracts.Client;
using OAuth.Contracts.Common;
using OAuth.Contracts.Requests;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Options;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/admin")]
[ApiVersion("1.0")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IUserQueryService _userQueryService;
    private readonly IClientService _clientService;
    private readonly IUserService _userService;
    private readonly IAdminRefreshTokenService _refreshTokenService;
    private readonly TokenOptions _tokenOptions;
    private readonly IJwtService _jwtService;

    public AdminController(
        IAdminService adminService,
        IUserQueryService userQueryService,
        IClientService clientService,
        IUserService userService,
        IAdminRefreshTokenService refreshTokenService,
        IOptions<TokenOptions> tokenOptions,
        IJwtService jwtService)
    {
        _adminService = adminService;
        _userQueryService = userQueryService;
        _clientService = clientService;
        _userService = userService;
        _refreshTokenService = refreshTokenService;
        _tokenOptions = tokenOptions.Value;
        _jwtService = jwtService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ApiResponse<AdminLoginResponse>> Login([FromBody] AdminLoginRequest request)
    {
        var admin = await _adminService.GetByUsernameAsync(request.Username);
        if (admin == null)
        {
            return new ApiResponse<AdminLoginResponse> { Code = 400, Message = "用户名或密码错误" };
        }

        var isValid = await _adminService.ValidatePasswordAsync(admin, request.Password);
        if (!isValid)
        {
            return new ApiResponse<AdminLoginResponse> { Code = 400, Message = "用户名或密码错误" };
        }

        admin.LastLoginAt = DateTime.UtcNow;
        await _adminService.UpdateAsync(admin);

        var token = _jwtService.GenerateAdminToken(admin);
        var refreshToken = await _refreshTokenService.CreateAsync(admin.Id);

        return new ApiResponse<AdminLoginResponse>
        {
            Data = new AdminLoginResponse
            {
                Id = admin.Id,
                Username = admin.Username,
                Role = admin.Role.ToString(),
                AvatarUrl = admin.AvatarUrl,
                AccessToken = token,
                RefreshToken = refreshToken.Token,
                ExpiresIn = _jwtService.GetExpirationSeconds(),
                RefreshExpiresIn = _tokenOptions.RefreshTokenExpirationSeconds
            }
        };
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ApiResponse<TokenRefreshResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var refreshToken = await _refreshTokenService.GetByTokenAsync(request.RefreshToken);

        if (refreshToken == null)
        {
            return new ApiResponse<TokenRefreshResponse> { Code = 401, Message = "无效的刷新令牌" };
        }

        if (!await _refreshTokenService.IsValidAsync(request.RefreshToken))
        {
            return new ApiResponse<TokenRefreshResponse> { Code = 401, Message = "刷新令牌已过期或已撤销" };
        }

        await _refreshTokenService.RevokeAsync(request.RefreshToken);

        var newAccessToken = _jwtService.GenerateAdminToken(refreshToken.Admin);
        var newRefreshToken = await _refreshTokenService.CreateAsync(refreshToken.AdminId);

        return new ApiResponse<TokenRefreshResponse>
        {
            Data = new TokenRefreshResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                TokenType = "Bearer",
                ExpiresIn = _jwtService.GetExpirationSeconds(),
                RefreshExpiresIn = _tokenOptions.RefreshTokenExpirationSeconds
            }
        };
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<DashboardResponse>> Dashboard()
    {
        var pendingClients = await _clientService.GetPendingAsync();
        var allClients = await _clientService.GetAllAsync();
        var totalUsers = await _userService.GetTotalUsersCount();

        return new ApiResponse<DashboardResponse>
        {
            Data = new DashboardResponse
            {
                PendingClients = pendingClients.Count,
                ApprovedClients = allClients.Count(c => c.Status == ClientStatus.Approved),
                TotalClients = allClients.Count,
                TotalUsers = totalUsers
            }
        };
    }

    [HttpPost("users")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<PagedResultResponse<UserDto>>> GetUsers([FromBody] PagedQueryRequest query)
    {
        var users = await _userQueryService.GetUsersAsync(query.Page, query.PageSize, query.Keyword);
        var total = await _userQueryService.GetTotalUsersCountAsync();

        return new ApiResponse<PagedResultResponse<UserDto>>
        {
            Data = new PagedResultResponse<UserDto>
            {
                Data = users.Select(MapToUserDto).ToList(),
                Total = total,
                Page = query.Page,
                PageSize = query.PageSize
            }
        };
    }

    [HttpPut("users/{id}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<UserDto>> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return new ApiResponse<UserDto> { Code = 404, Message = "用户未找到" };
        }

        user.Status = request.Status;
        await _userService.UpdateAsync(user);

        return new ApiResponse<UserDto>
        {
            Data = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                Status = user.Status.ToString(),
                TwoFactorEnabled = user.TwoFactorEnabled,
                EmailVerified = user.EmailVerified,
                CreatedAt = user.CreatedAt
            },
            Message = "用户状态已更新"
        };
    }

    [HttpDelete("users/{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ApiResponse<UserDto>> DeleteUser(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user != null)
        {
            var response = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                Status = user.Status.ToString(),
                TwoFactorEnabled = user.TwoFactorEnabled,
                EmailVerified = user.EmailVerified,
                CreatedAt = user.CreatedAt
            };
            await _userQueryService.DeleteUserAsync(id);
            return new ApiResponse<UserDto> { Data = response, Message = "用户已删除" };
        }
        return new ApiResponse<UserDto> { Message = "用户已删除" };
    }

    [HttpGet("clients")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<PagedResultResponse<ClientDto>>> GetClients([FromQuery] PagedQueryRequest query)
    {
        var clients = await _clientService.GetAllAsync();

        if (!string.IsNullOrEmpty(query.Keyword))
        {
            clients = clients.Where(c => c.Name.Contains(query.Keyword) || c.ClientId.Contains(query.Keyword)).ToList();
        }

        var total = clients.Count;
        var paginated = clients.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();

        return new ApiResponse<PagedResultResponse<ClientDto>>
        {
            Data = new PagedResultResponse<ClientDto>
            {
                Data = paginated.Select(MapToClientDto).ToList(),
                Total = total,
                Page = query.Page,
                PageSize = query.PageSize
            }
        };
    }

    [HttpPut("clients/{id}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<ClientDto>> ApproveClient(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return new ApiResponse<ClientDto> { Code = 404, Message = "客户端未找到" };
        }

        client.Status = ClientStatus.Approved;
        await _clientService.UpdateAsync(client);

        return new ApiResponse<ClientDto>
        {
            Data = new ClientDto
            {
                Id = client.Id,
                Name = client.Name,
                ClientId = client.ClientId,
                Status = client.Status.ToString(),
                RedirectUris = client.RedirectUris,
                AllowedScopes = client.AllowedScopes,
                CreatedAt = client.CreatedAt
            },
            Message = "客户端已批准"
        };
    }

    [HttpPut("clients/{id}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<ClientDto>> RejectClient(Guid id, [FromBody] RejectClientRequest request)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return new ApiResponse<ClientDto> { Code = 404, Message = "客户端未找到" };
        }

        client.Status = ClientStatus.Rejected;

        if (!string.IsNullOrWhiteSpace(request?.Reason))
        {
            client.Description = string.IsNullOrEmpty(client.Description)
                ? $"[拒绝原因] {request.Reason}"
                : $"{client.Description}\n[拒绝原因] {request.Reason}";
        }

        await _clientService.UpdateAsync(client);

        return new ApiResponse<ClientDto>
        {
            Data = new ClientDto
            {
                Id = client.Id,
                Name = client.Name,
                ClientId = client.ClientId,
                Status = client.Status.ToString(),
                RedirectUris = client.RedirectUris,
                AllowedScopes = client.AllowedScopes,
                CreatedAt = client.CreatedAt
            },
            Message = "客户端已拒绝"
        };
    }

    [HttpDelete("clients/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<ClientDto>> DeleteClient(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client != null)
        {
            var response = new ClientDto
            {
                Id = client.Id,
                Name = client.Name,
                ClientId = client.ClientId,
                Status = client.Status.ToString(),
                RedirectUris = client.RedirectUris,
                AllowedScopes = client.AllowedScopes,
                CreatedAt = client.CreatedAt
            };
            await _clientService.DeleteAsync(id);
            return new ApiResponse<ClientDto> { Data = response, Message = "客户端已删除" };
        }
        return new ApiResponse<ClientDto> { Message = "客户端已删除" };
    }

    [HttpPost("admins")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ApiResponse<AdminCreatedResponse>> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        var existingAdmin = await _adminService.GetByUsernameAsync(request.Username);
        if (existingAdmin != null)
        {
            return new ApiResponse<AdminCreatedResponse> { Code = 400, Message = "用户名已存在" };
        }

        var admin = await _adminService.CreateAsync(request.Username, request.Password, request.Role);

        return new ApiResponse<AdminCreatedResponse>
        {
            Data = new AdminCreatedResponse
            {
                Id = admin.Id,
                Username = admin.Username,
                Role = admin.Role.ToString()
            }
        };
    }

    private static UserDto MapToUserDto(User u) => new UserDto
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        Phone = u.Phone,
        Status = u.Status.ToString(),
        TwoFactorEnabled = u.TwoFactorEnabled,
        EmailVerified = u.EmailVerified,
        CreatedAt = u.CreatedAt
    };

    private static ClientDto MapToClientDto(Client c) => new ClientDto
    {
        Id = c.Id,
        Name = c.Name,
        ClientId = c.ClientId,
        Description = c.Description,
        Status = c.Status.ToString(),
        RedirectUris = c.RedirectUris,
        AllowedScopes = c.AllowedScopes,
        CreatedAt = c.CreatedAt
    };
}