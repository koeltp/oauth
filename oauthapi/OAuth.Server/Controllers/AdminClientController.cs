using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Application.Mappers;
using OAuth.Contracts.Client;
using OAuth.Contracts.Common;
using Taipi.Core.RQRS;
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

    [HttpPost("clients")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<PagerResponseResult<ClientDto>> GetClients([FromBody] SearchPager<ClientSearchDto> query)
    {
        var result = await _clientService.GetListAsync(query);
        return new PagerResponseResult<ClientDto>(result.Items, query.PageIndex,query.PageSize, result.TotalCount);
    }

    [HttpPut("clients/{id}/approve")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ResponseResult<ClientDto>> ApproveClient(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return ResponseResult<ClientDto>.NotFound("客户端未找到");
        }

        client.Status = ClientStatus.Approved;
        await _clientService.UpdateAsync(client);

        return new ResponseResult<ClientDto>(client.ToDto()) { Message = "客户端已批准" };
    }

    [HttpPut("clients/{id}/reject")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ResponseResult<ClientDto>> RejectClient(Guid id, [FromBody] RejectClientRequest request)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return ResponseResult<ClientDto>.NotFound("客户端未找到");
        }

        client.Status = ClientStatus.Rejected;

        if (!string.IsNullOrWhiteSpace(request?.Reason))
        {
            client.Description = string.IsNullOrEmpty(client.Description)
                ? $"[拒绝原因] {request.Reason}"
                : $"{client.Description}\n[拒绝原因] {request.Reason}";
        }

        await _clientService.UpdateAsync(client);

        return new ResponseResult<ClientDto>(client.ToDto()) { Message = "客户端已拒绝" };
    }

    [HttpDelete("clients/{id}")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ResponseResult<ClientDto>> DeleteClient(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return ResponseResult<ClientDto>.NotFound("客户端未找到");
        }

        var dto = client.ToDto();
        await _clientService.DeleteAsync(id);
        return new ResponseResult<ClientDto>(dto) { Message = "客户端已删除" };
    }
}