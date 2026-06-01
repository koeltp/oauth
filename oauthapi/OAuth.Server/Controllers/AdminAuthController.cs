using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Admin;
using OAuth.Contracts.Auth;
using OAuth.Contracts.Common;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Options;
using Taipi.Core.RQRS;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/admin")]
[ApiVersion("1.0")]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IAdminRefreshTokenService _refreshTokenService;
    private readonly IJwtService _jwtService;
    private readonly TokenOptions _tokenOptions;

    public AdminAuthController(
        IAdminService adminService,
        IAdminRefreshTokenService refreshTokenService,
        IJwtService jwtService,
        IOptions<TokenOptions> tokenOptions)
    {
        _adminService = adminService;
        _refreshTokenService = refreshTokenService;
        _jwtService = jwtService;
        _tokenOptions = tokenOptions.Value;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ResponseResult<AdminLoginResponse>> Login([FromBody] AdminLoginRequest request)
    {
        var admin = await _adminService.GetByUsernameAsync(request.Username);
        if (admin == null)
        {
            return ResponseResult<AdminLoginResponse>.BadRequest("用户名或密码错误");
        }

        var isValid = await _adminService.ValidatePasswordAsync(admin, request.Password);
        if (!isValid)
        {
            return ResponseResult<AdminLoginResponse>.BadRequest("用户名或密码错误");
        }

        if (admin.Status != AdminStatus.Active)
        {
            return ResponseResult<AdminLoginResponse>.BadRequest("管理员账户已禁用");
        }

        admin.LastLoginAt = DateTime.UtcNow;
        await _adminService.UpdateAsync(admin);

        var token = _jwtService.GenerateAdminToken(admin);
        var refreshToken = await _refreshTokenService.CreateAsync(admin.Id);

        return new ResponseResult<AdminLoginResponse>(new AdminLoginResponse
        {
            Id = admin.Id,
            Username = admin.Username,
            Role = admin.Role.ToString(),
            AvatarUrl = admin.AvatarUrl,
            AccessToken = token,
            RefreshToken = refreshToken.Token,
            ExpiresIn = _jwtService.GetExpirationSeconds(),
            RefreshExpiresIn = _tokenOptions.RefreshTokenExpirationSeconds
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ResponseResult<TokenRefreshResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var refreshToken = await _refreshTokenService.GetByTokenAsync(request.RefreshToken);

        if (refreshToken == null)
        {
            return ResponseResult<TokenRefreshResponse>.Unauthorized("无效的刷新令牌");
        }

        if (!await _refreshTokenService.IsValidAsync(request.RefreshToken))
        {
            return ResponseResult<TokenRefreshResponse>.Unauthorized("刷新令牌已过期或已撤销");
        }

        await _refreshTokenService.RevokeAsync(request.RefreshToken);

        var newAccessToken = _jwtService.GenerateAdminToken(refreshToken.Admin);
        var newRefreshToken = await _refreshTokenService.CreateAsync(refreshToken.AdminId);

        return new ResponseResult<TokenRefreshResponse>(new TokenRefreshResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            TokenType = "Bearer",
            ExpiresIn = _jwtService.GetExpirationSeconds(),
            RefreshExpiresIn = _tokenOptions.RefreshTokenExpirationSeconds
        });
    }
}