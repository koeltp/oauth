using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Application.Mappers;
using OAuth.Contracts.Client;
using OAuth.Contracts.Common;
using OAuth.Contracts.Requests;
using OAuth.Domain.Entities;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/admin")]
[ApiVersion("1.0")]
public class AdminClientController : ControllerBase
{
    private readonly IClientService _clientService;

    public AdminClientController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet("clients")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ApiResponse<PagedResultResponse<ClientDto>>> GetClients([FromQuery] PagedQueryRequest query)
    {
        var clients = await _clientService.GetAllAsync();

        if (!string.IsNullOrEmpty(query.Keyword))
        {
            clients = clients.Where(c => c.Name.Contains(query.Keyword) || c.ClientId.Contains(query.Keyword)).ToList();
        }

        var total = clients.Count;
        var paginated = clients.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();

        return ApiResponse<PagedResultResponse<ClientDto>>.Success(new PagedResultResponse<ClientDto>
        {
            Data = paginated.Select(c => c.ToDto()).ToList(),
            Total = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    [HttpPut("clients/{id}/approve")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ApiResponse<ClientDto>> ApproveClient(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return ApiResponse<ClientDto>.NotFound("客户端未找到");
        }

        client.Status = ClientStatus.Approved;
        await _clientService.UpdateAsync(client);

        return ApiResponse<ClientDto>.Success("客户端已批准", client.ToDto());
    }

    [HttpPut("clients/{id}/reject")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ApiResponse<ClientDto>> RejectClient(Guid id, [FromBody] RejectClientRequest request)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return ApiResponse<ClientDto>.NotFound("客户端未找到");
        }

        client.Status = ClientStatus.Rejected;

        if (!string.IsNullOrWhiteSpace(request?.Reason))
        {
            client.Description = string.IsNullOrEmpty(client.Description)
                ? $"[拒绝原因] {request.Reason}"
                : $"{client.Description}\n[拒绝原因] {request.Reason}";
        }

        await _clientService.UpdateAsync(client);

        return ApiResponse<ClientDto>.Success("客户端已拒绝", client.ToDto());
    }

    [HttpDelete("clients/{id}")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ApiResponse<ClientDto>> DeleteClient(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return ApiResponse<ClientDto>.NotFound("客户端未找到");
        }

        var dto = client.ToDto();
        await _clientService.DeleteAsync(id);
        return ApiResponse<ClientDto>.Success("客户端已删除", dto);
    }
}