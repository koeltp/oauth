using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Application.Mappers;
using OAuth.Contracts.Admin;
using OAuth.Contracts.Common;
using OAuth.Contracts.Requests;
using OAuth.Domain.Entities;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/admin")]
[ApiVersion("1.0")]
public class AdminManagementController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminManagementController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpPost("admins/list")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<ApiResponse<PagedResultResponse<AdminDto>>> GetAdmins([FromBody] PagedQueryRequest query)
    {
        var result = await _adminService.GetAllAsync(query.Page, query.PageSize, query.Keyword);
        return ApiResponse<PagedResultResponse<AdminDto>>.Success(result);
    }

    [HttpPost("admins")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<ApiResponse<AdminCreatedResponse>> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        var existingAdmin = await _adminService.GetByUsernameAsync(request.Username);
        if (existingAdmin != null)
        {
            return ApiResponse<AdminCreatedResponse>.Error(400, "用户名已存在");
        }

        var admin = await _adminService.CreateAsync(request.Username, request.Password, request.Role);

        return ApiResponse<AdminCreatedResponse>.Success(new AdminCreatedResponse
        {
            Id = admin.Id,
            Username = admin.Username,
            Role = admin.Role.ToString()
        });
    }

    [HttpPut("admins/{id}")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<ApiResponse<AdminDto>> UpdateAdmin(Guid id, [FromBody] UpdateAdminRequest request)
    {
        var admin = await _adminService.GetByIdAsync(id);
        if (admin == null)
        {
            return ApiResponse<AdminDto>.NotFound("管理员未找到");
        }

        if (!string.IsNullOrEmpty(request.Email))
            admin.Email = request.Email;

        if (request.Role.HasValue)
            admin.Role = request.Role.Value;

        admin.UpdatedAt = DateTime.UtcNow;
        await _adminService.UpdateAsync(admin);

        return ApiResponse<AdminDto>.Success("管理员已更新", admin.ToDto());
    }

    [HttpDelete("admins/{id}")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<ApiResponse<object>> DeleteAdmin(Guid id)
    {
        var admin = await _adminService.GetByIdAsync(id);
        if (admin == null)
        {
            return ApiResponse<object>.NotFound("管理员未找到");
        }

        await _adminService.DeleteAsync(id);
        return ApiResponse<object>.Success("管理员已删除");
    }

    [HttpPut("admins/{id}/status")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<ApiResponse<AdminDto>> UpdateAdminStatus(Guid id, [FromBody] UpdateAdminStatusRequest request)
    {
        var admin = await _adminService.GetByIdAsync(id);
        if (admin == null)
        {
            return ApiResponse<AdminDto>.NotFound("管理员未找到");
        }

        admin.Status = request.Status;
        admin.UpdatedAt = DateTime.UtcNow;
        await _adminService.UpdateAsync(admin);

        return ApiResponse<AdminDto>.Success("管理员状态已更新", admin.ToDto());
    }
}