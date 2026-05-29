using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Admin;
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
    private readonly IClientService _clientService;
    private readonly IUserService _userService;
    private readonly IAdminRefreshTokenService _refreshTokenService;
    private readonly TokenOptions _tokenOptions;
    private readonly IJwtService _jwtService;

    public AdminController(
        IAdminService adminService,
        IClientService clientService,
        IUserService userService,
        IAdminRefreshTokenService refreshTokenService,
        IOptions<TokenOptions> tokenOptions,
        IJwtService jwtService)
    {
        _adminService = adminService;
        _clientService = clientService;
        _userService = userService;
        _refreshTokenService = refreshTokenService;
        _tokenOptions = tokenOptions.Value;
        _jwtService = jwtService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
    {
        var admin = await _adminService.GetByUsernameAsync(request.Username);
        if (admin == null)
        {
            return BadRequest(ApiResponse<object>.BadRequest("用户名或密码错误"));
        }

        var isValid = await _adminService.ValidatePasswordAsync(admin, request.Password);
        if (!isValid)
        {
            return BadRequest(ApiResponse<object>.BadRequest("用户名或密码错误"));
        }

        // 更新最后登录时间
        admin.LastLoginAt = DateTime.UtcNow;
        await _adminService.UpdateAsync(admin);

        // 生成 access token
        var token = _jwtService.GenerateAdminToken(admin);

        // 生成 refresh token
        var refreshToken = await _refreshTokenService.CreateAsync(admin.Id);

        var result = new
        {
            id = admin.Id,
            username = admin.Username,
            role = admin.Role.ToString(),
            avatarUrl = admin.AvatarUrl,
            access_token = token,
            refresh_token = refreshToken.Token,
            token_type = "Bearer",
            expires_in = _jwtService.GetExpirationSeconds(),
            refresh_expires_in = _tokenOptions.RefreshTokenExpirationSeconds
        };

        return Ok(ApiResponse<object>.Success(result));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var refreshToken = await _refreshTokenService.GetByTokenAsync(request.RefreshToken);

        if (refreshToken == null)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }

        if (!await _refreshTokenService.IsValidAsync(request.RefreshToken))
        {
            return Unauthorized(new { message = "Refresh token expired or revoked" });
        }

        // 撤销旧的 refresh token
        await _refreshTokenService.RevokeAsync(request.RefreshToken);

        // 生成新的 access token
        var newAccessToken = _jwtService.GenerateAdminToken(refreshToken.Admin);

        // 生成新的 refresh token
        var newRefreshToken = await _refreshTokenService.CreateAsync(refreshToken.AdminId);

        return Ok(new
        {
            access_token = newAccessToken,
            refresh_token = newRefreshToken.Token,
            token_type = "Bearer",
            expires_in = _jwtService.GetExpirationSeconds(),
            refresh_expires_in = _tokenOptions.RefreshTokenExpirationSeconds
        });
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Dashboard()
    {
        var pendingClients = await _clientService.GetPendingAsync();
        var allClients = await _clientService.GetAllAsync();
        var totalUsers = await _userService.GetTotalUsersCount();

        return Ok(new
        {
            pending_clients = pendingClients.Count,
            approved_clients = allClients.Count(c => c.Status == ClientStatus.Approved),
            total_clients = allClients.Count,
            total_users = totalUsers
        });
    }

    [HttpPost("users")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUsers([FromBody] PagedQueryRequest query)
    {
        var users = await _adminService.GetUsersAsync(query.Page, query.PageSize, query.Keyword);
        var total = await _adminService.GetTotalUsersCount();

        return Ok(new
        {
            data = users.Select(MapToUserDto),
            total,
            page = query.Page,
            page_size = query.PageSize
        });
    }

    [HttpPut("users/{id}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        user.Status = request.Status;
        await _userService.UpdateAsync(user);

        return Ok(new { message = "User status updated" });
    }

    [HttpDelete("users/{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _adminService.DeleteUserAsync(id);
        return Ok(new { message = "User deleted" });
    }

    [HttpGet("clients")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetClients([FromQuery] PagedQueryRequest query)
    {
        var clients = await _clientService.GetAllAsync();

        if (!string.IsNullOrEmpty(query.Keyword))
        {
            clients = clients.Where(c => c.Name.Contains(query.Keyword) || c.ClientId.Contains(query.Keyword)).ToList();
        }

        var total = clients.Count;
        var paginated = clients.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();

        return Ok(new
        {
            data = paginated.Select(MapToClientDto),
            total,
            page = query.Page,
            page_size = query.PageSize
        });
    }

    [HttpPut("clients/{id}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ApproveClient(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return NotFound(new { message = "Client not found" });
        }

        client.Status = ClientStatus.Approved;
        await _clientService.UpdateAsync(client);

        return Ok(new { message = "Client approved" });
    }

    [HttpPut("clients/{id}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RejectClient(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return NotFound(new { message = "Client not found" });
        }

        client.Status = ClientStatus.Rejected;
        await _clientService.UpdateAsync(client);

        return Ok(new { message = "Client rejected" });
    }

    [HttpDelete("clients/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        await _clientService.DeleteAsync(id);
        return Ok(new { message = "Client deleted" });
    }

    [HttpPost("admins")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        var existingAdmin = await _adminService.GetByUsernameAsync(request.Username);
        if (existingAdmin != null)
        {
            return BadRequest(new { message = "Username already exists" });
        }

        var admin = await _adminService.CreateAsync(request.Username, request.Password, request.Role);

        return Ok(new
        {
            id = admin.Id,
            username = admin.Username,
            role = admin.Role.ToString()
        });
    }

    private static object MapToUserDto(User u) => new
    {
        id = u.Id,
        username = u.Username,
        email = u.Email,
        phone = u.Phone,
        status = u.Status.ToString(),
        two_factor_enabled = u.TwoFactorEnabled,
        email_verified = u.EmailVerified,
        created_at = u.CreatedAt
    };

    private static object MapToClientDto(Client c) => new
    {
        id = c.Id,
        name = c.Name,
        client_id = c.ClientId,
        status = c.Status.ToString(),
        redirect_uri = c.RedirectUris,
        created_at = c.CreatedAt
    };
}