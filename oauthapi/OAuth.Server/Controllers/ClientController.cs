using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Client;
using OAuth.Contracts.Common;
using OAuth.Domain.Entities;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/clients")]
[ApiVersion("1.0")]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEncryptionService _encryptionService;

    public ClientController(IClientService clientService, ICurrentUserService currentUserService, IEncryptionService encryptionService)
    {
        _clientService = clientService;
        _currentUserService = currentUserService;
        _encryptionService = encryptionService;
    }

    [HttpGet]
    public async Task<ApiResponse<IEnumerable<ClientResponse>>> GetAll()
    {
        var clients = await _clientService.GetAllAsync();
        return new ApiResponse<IEnumerable<ClientResponse>>
        {
            Data = clients.Select(c => ToResponse(c))
        };
    }

    [HttpGet("pending")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ApiResponse<IEnumerable<ClientResponse>>> GetPending()
    {
        var clients = await _clientService.GetPendingAsync();
        return new ApiResponse<IEnumerable<ClientResponse>>
        {
            Data = clients.Select(c => ToResponse(c))
        };
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<ClientResponse>> GetById(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return new ApiResponse<ClientResponse> { Code = 404, Message = "客户端未找到" };
        }
        return new ApiResponse<ClientResponse> { Data = ToResponse(client) };
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ApiResponse<ClientRegisteredResponse>> Register([FromBody] RegisterClientRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var (client, clientSecret) = await _clientService.CreateAsync(
            request.Name,
            request.Description,
            request.RedirectUris,
            request.AllowedScopes,
            userId);

        // 注册后自动提交为待审核
        await _clientService.SubmitAsync(client.Id);

        return new ApiResponse<ClientRegisteredResponse>
        {
            Data = new ClientRegisteredResponse
            {
                ClientId = client.ClientId,
                ClientSecret = clientSecret,
                Status = ClientStatus.Pending.ToString(),
                Message = "客户端注册成功，等待审核"
            }
        };
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<ApiResponse<IEnumerable<ClientResponse>>> GetMyClients()
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return new ApiResponse<IEnumerable<ClientResponse>> { Code = 401, Message = "未授权" };
        }

        var clients = await _clientService.GetByUserIdAsync(userId.Value);
        return new ApiResponse<IEnumerable<ClientResponse>>
        {
            Data = clients.Select(c => ToResponse(c))
        };
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ApiResponse<ClientResponse>> Update(Guid id, [FromBody] UpdateClientRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return new ApiResponse<ClientResponse> { Code = 404, Message = "客户端未找到" };
        }
        if (client.UserId != userId)
        {
            return new ApiResponse<ClientResponse> { Code = 403, Message = "无权修改此客户端" };
        }
        if (client.Status != ClientStatus.Draft)
        {
            return new ApiResponse<ClientResponse> { Code = 400, Message = "仅草稿状态下可编辑，请先撤回" };
        }

        client.Name = request.Name;
        client.Description = request.Description;
        client.RedirectUris = request.RedirectUris;
        client.AllowedScopes = request.AllowedScopes;
        await _clientService.UpdateAsync(client);

        return new ApiResponse<ClientResponse> { Data = ToResponse(client), Message = "更新成功" };
    }

    [HttpPost("{id}/submit")]
    [Authorize]
    public async Task<ApiResponse<ClientResponse>> Submit(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return new ApiResponse<ClientResponse> { Code = 404, Message = "客户端未找到" };
        }
        if (client.UserId != userId)
        {
            return new ApiResponse<ClientResponse> { Code = 403, Message = "无权操作此客户端" };
        }
        if (client.Status != ClientStatus.Draft)
        {
            return new ApiResponse<ClientResponse> { Code = 400, Message = "仅草稿状态可提交审核" };
        }

        await _clientService.SubmitAsync(id);
        client.Status = ClientStatus.Pending;
        return new ApiResponse<ClientResponse> { Data = ToResponse(client), Message = "已提交审核" };
    }

    [HttpPost("{id}/withdraw")]
    [Authorize]
    public async Task<ApiResponse<ClientResponse>> Withdraw(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return new ApiResponse<ClientResponse> { Code = 404, Message = "客户端未找到" };
        }
        if (client.UserId != userId)
        {
            return new ApiResponse<ClientResponse> { Code = 403, Message = "无权操作此客户端" };
        }
        if (client.Status == ClientStatus.Draft)
        {
            return new ApiResponse<ClientResponse> { Code = 400, Message = "已是草稿状态" };
        }

        await _clientService.WithdrawAsync(id);
        client.Status = ClientStatus.Draft;
        return new ApiResponse<ClientResponse> { Data = ToResponse(client), Message = "已撤回，可重新编辑" };
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ApiResponse<object>> Delete(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return new ApiResponse<object> { Code = 404, Message = "客户端未找到" };
        }

        // 允许所有者和管理员删除
        var isAdmin = User.IsInRole("Admin");
        if (client.UserId != userId && !isAdmin)
        {
            return new ApiResponse<object> { Code = 403, Message = "无权删除此客户端" };
        }

        await _clientService.DeleteAsync(id);
        return new ApiResponse<object> { Message = "客户端已成功删除" };
    }

    [HttpPost("{id}/approve")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ApiResponse<ClientResponse>> Approve(Guid id)
    {
        var adminId = _currentUserService.GetUserId();
        if (adminId == null)
        {
            return new ApiResponse<ClientResponse> { Code = 401, Message = "未授权" };
        }

        await _clientService.ApproveAsync(id, adminId.Value);
        var client = await _clientService.GetByIdAsync(id);
        return new ApiResponse<ClientResponse>
        {
            Data = ToResponse(client),
            Message = "客户端已批准"
        };
    }

    [HttpPost("{id}/reject")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ApiResponse<ClientResponse>> Reject(Guid id)
    {
        var adminId = _currentUserService.GetUserId();
        if (adminId == null)
        {
            return new ApiResponse<ClientResponse> { Code = 401, Message = "未授权" };
        }

        await _clientService.RejectAsync(id, adminId.Value);
        var client = await _clientService.GetByIdAsync(id);
        return new ApiResponse<ClientResponse>
        {
            Data = ToResponse(client),
            Message = "客户端已拒绝"
        };
    }

    private ClientResponse ToResponse(Client client)
    {
        var clientSecret = client.Status == ClientStatus.Approved && !string.IsNullOrEmpty(client.ClientSecretEncrypted)
            ? _encryptionService.Decrypt(client.ClientSecretEncrypted)
            : string.Empty;

        return new ClientResponse
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientSecret = clientSecret,
            Name = client.Name,
            Logo = client.Logo,
            Description = client.Description,
            RedirectUris = client.RedirectUris,
            AllowedScopes = client.AllowedScopes,
            Status = client.Status.ToString(),
            CreatedAt = client.CreatedAt
        };
    }
}