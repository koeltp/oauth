using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Admin;
using OAuth.Contracts.Client;
using OAuth.Contracts.Common;
using Taipi.Core.RQRS;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/admin")]
[ApiVersion("1.0")]
public class AdminDashboardController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly IUserService _userService;

    public AdminDashboardController(IClientService clientService, IUserService userService)
    {
        _clientService = clientService;
        _userService = userService;
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ResponseResult<DashboardResponse>> Dashboard()
    {
        var pendingCount = await _clientService.GetClientsCountByStatus(Domain.Entities.ClientStatus.Pending);
        var approvedCount = await _clientService.GetClientsCountByStatus(Domain.Entities.ClientStatus.Approved);
        var totalClients = await _clientService.GetTotalClientsCount();
        var totalUsers = await _userService.GetTotalUsersCount();

        return new ResponseResult<DashboardResponse>(new DashboardResponse
        {
            PendingClients = pendingCount,
            ApprovedClients = approvedCount,
            TotalClients = totalClients,
            TotalUsers = totalUsers
        });
    }

    /// <summary>
    /// 最近活动记录
    /// </summary>
    /// <returns></returns>
    [HttpGet("dashboard/recent-activities")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ResponseResult<List<RecentActivityResponse>>> GetRecentActivities()
    {
        var activities = new List<RecentActivityResponse>();

        var result = await _clientService.GetListAsync(new SearchPager<ClientSearchDto>
        {
            PageIndex = 1,
            PageSize = 10
        });

        var clientActivities = result.Items
            .Where(c => c.UpdatedAt.HasValue || c.Status != "Draft")
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Take(10)
            .Select(c =>
            {
                var action = c.Status switch
                {
                    "Pending" => "提交审核",
                    "Approved" => "已批准",
                    "Rejected" => "已拒绝",
                    _ => "已更新"
                };
                return new RecentActivityResponse
                {
                    Action = $"客户端 {action}",
                    Description = $"客户端 \"{c.Name}\" {action}",
                    AdminName = c.ReviewerName ?? "系统",
                    CreatedAt = c.UpdatedAt ?? c.CreatedAt
                };
            });

        activities.AddRange(clientActivities);

        var recentUsers = await _userService.GetRecentUsersAsync(5);
        var userActivities = recentUsers.Select(u => new RecentActivityResponse
        {
            Action = "用户注册",
            Description = $"新用户 \"{u.Username}\" 注册",
            AdminName = "系统",
            CreatedAt = u.CreatedAt
        });

        activities.AddRange(userActivities);

        activities = activities.OrderByDescending(a => a.CreatedAt).Take(10).ToList();

        return new ResponseResult<List<RecentActivityResponse>>(activities);
    }
}