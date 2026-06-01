using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Application.Mappers;
using OAuth.Contracts.Admin;
using OAuth.Contracts.Common;
using OAuth.Domain.Entities;
using Taipi.Core.RQRS;

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
    public async Task<ResponseResult<PagerResponse<AdminDto>>> GetAdmins([FromBody] SearchPager<string?> query)
    {
        var result = await _adminService.GetListAsync(query);
        return new ResponseResult<PagerResponse<AdminDto>>(result);
    }

    [HttpPost("admins")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<ResponseResult<AdminCreatedResponse>> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        var existingAdmin = await _adminService.GetByUsernameAsync(request.Username);
        if (existingAdmin != null)
        {
            return ResponseResult<AdminCreatedResponse>.Error(400, "用户名已存在");
        }

        var admin = await _adminService.CreateAsync(request.Username, request.Password, request.Role);

        return new ResponseResult<AdminCreatedResponse>(new AdminCreatedResponse
        {
            Id = admin.Id,
            Username = admin.Username,
            Role = admin.Role.ToString()
        });
    }

    [HttpPut("admins/{id}")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<ResponseResult<AdminDto>> UpdateAdmin(Guid id, [FromBody] UpdateAdminRequest request)
    {
        var admin = await _adminService.GetByIdAsync(id);
        if (admin == null)
        {
            return ResponseResult<AdminDto>.NotFound("管理员未找到");
        }

        if (!string.IsNullOrEmpty(request.Email))
            admin.Email = request.Email;

        if (request.Role.HasValue)
            admin.Role = request.Role.Value;

        admin.UpdatedAt = DateTime.UtcNow;
        await _adminService.UpdateAsync(admin);

        return new ResponseResult<AdminDto>(admin.ToDto()) { Message = "管理员已更新" };
    }

    [HttpDelete("admins/{id}")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<ResponseResult<object>> DeleteAdmin(Guid id)
    {
        var admin = await _adminService.GetByIdAsync(id);
        if (admin == null)
        {
            return ResponseResult<object>.NotFound("管理员未找到");
        }

        await _adminService.DeleteAsync(id);
        return ResponseResult<object>.Success("管理员已删除");
    }

    [HttpPut("admins/{id}/status")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<ResponseResult<AdminDto>> UpdateAdminStatus(Guid id, [FromBody] UpdateAdminStatusRequest request)
    {
        var admin = await _adminService.GetByIdAsync(id);
        if (admin == null)
        {
            return ResponseResult<AdminDto>.NotFound("管理员未找到");
        }

        admin.Status = request.Status;
        admin.UpdatedAt = DateTime.UtcNow;
        await _adminService.UpdateAsync(admin);

        return new ResponseResult<AdminDto>(admin.ToDto()) { Message = "管理员状态已更新" };
    }
}