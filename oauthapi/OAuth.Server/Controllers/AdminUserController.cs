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
public class AdminUserController : ControllerBase
{
    private readonly IUserQueryService _userQueryService;
    private readonly IUserService _userService;

    public AdminUserController(IUserQueryService userQueryService, IUserService userService)
    {
        _userQueryService = userQueryService;
        _userService = userService;
    }

    [HttpPost("users")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ApiResponse<PagedResultResponse<UserDto>>> GetUsers([FromBody] PagedQueryRequest query)
    {
        var users = await _userQueryService.GetUsersAsync(query.Page, query.PageSize, query.Keyword);
        var total = await _userQueryService.GetTotalUsersCountAsync();

        return ApiResponse<PagedResultResponse<UserDto>>.Success(new PagedResultResponse<UserDto>
        {
            Data = users.Select(u => u.ToDto()).ToList(),
            Total = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    [HttpPut("users/{id}/status")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ApiResponse<UserDto>> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return ApiResponse<UserDto>.NotFound("用户未找到");
        }

        user.Status = request.Status;
        await _userService.UpdateAsync(user);

        return ApiResponse<UserDto>.Success("用户状态已更新", user.ToDto());
    }

    [HttpDelete("users/{id}")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<ApiResponse<UserDto>> DeleteUser(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return ApiResponse<UserDto>.NotFound("用户未找到");
        }

        var dto = user.ToDto();
        await _userQueryService.DeleteUserAsync(id);
        return ApiResponse<UserDto>.Success("用户已删除", dto);
    }
}