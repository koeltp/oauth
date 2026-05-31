using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Admin;
using OAuth.Contracts.Common;
using OAuth.Domain.Entities;

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
    public async Task<ApiResponse<DashboardResponse>> Dashboard()
    {
        var pendingClients = await _clientService.GetPendingAsync();
        var allClients = await _clientService.GetAllAsync();
        var totalUsers = await _userService.GetTotalUsersCount();

        return ApiResponse<DashboardResponse>.Success(new DashboardResponse
        {
            PendingClients = pendingClients.Count,
            ApprovedClients = allClients.Count(c => c.Status == ClientStatus.Approved),
            TotalClients = allClients.Count,
            TotalUsers = totalUsers
        });
    }

    [HttpGet("dashboard/recent-activities")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ApiResponse<List<RecentActivityResponse>>> GetRecentActivities()
    {
        var activities = new List<RecentActivityResponse>();

        var recentClients = await _clientService.GetAllAsync();
        var clientActivities = recentClients
            .Where(c => c.UpdatedAt.HasValue || c.Status != ClientStatus.Draft)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Take(10)
            .Select(c =>
            {
                var action = c.Status switch
                {
                    ClientStatus.Pending => "提交审核",
                    ClientStatus.Approved => "已批准",
                    ClientStatus.Rejected => "已拒绝",
                    _ => "已更新"
                };
                return new RecentActivityResponse
                {
                    Action = $"客户端 {action}",
                    Description = $"客户端 \"{c.Name}\" {action}",
                    AdminName = c.Reviewer?.Username ?? "系统",
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

        return ApiResponse<List<RecentActivityResponse>>.Success(activities);
    }
}