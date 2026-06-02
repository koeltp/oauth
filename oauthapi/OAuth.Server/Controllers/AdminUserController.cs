using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Application.Mappers;
using OAuth.Contracts.Admin;
using OAuth.Contracts.Common;
using Taipi.Core.RQRS;
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
    public async Task<ResponseResult<PagerResponse<UserDto>>> GetUsers([FromBody] SearchPager<string?> query)
    {
        var users = await _userQueryService.GetUsersAsync(query);
        var total = await _userQueryService.GetTotalUsersCountAsync();

        return new ResponseResult<PagerResponse<UserDto>>(new PagerResponse<UserDto>
        {
            Items = users.Select(u => u.ToDto()),
            TotalCount = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        });
    }

    [HttpPut("users/{id}/status")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ResponseResult<UserDto>> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return ResponseResult<UserDto>.NotFound("用户未找到");
        }

        user.Status = request.Status;
        await _userService.UpdateAsync(user);

        return new ResponseResult<UserDto>(user.ToDto()) { Message = "用户状态已更新" };
    }

    [HttpDelete("users/{id}")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<ResponseResult<UserDto>> DeleteUser(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return ResponseResult<UserDto>.NotFound("用户未找到");
        }

        var dto = user.ToDto();
        await _userQueryService.DeleteUserAsync(id);
        return new ResponseResult<UserDto>(dto) { Message = "用户已删除" };
    }
}