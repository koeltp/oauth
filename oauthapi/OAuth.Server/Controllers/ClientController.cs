using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Client;
using OAuth.Contracts.Common;
using OAuth.Domain.Entities;
using Taipi.Core.RQRS;

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

    [HttpGet("pending")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ResponseResult<IEnumerable<ClientResponse>>> GetPending()
    {
        var clients = await _clientService.GetPendingAsync();
        return new ResponseResult<IEnumerable<ClientResponse>>(clients.Select(c => ToResponse(c)));
    }

    [HttpGet("{id}")]
    public async Task<ResponseResult<ClientResponse>> GetById(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return ResponseResult<ClientResponse>.NotFound("客户端未找到");
        }
        return new ResponseResult<ClientResponse>(ToResponse(client));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ResponseResult<ClientRegisteredResponse>> Register([FromBody] RegisterClientRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var (client, clientSecret) = await _clientService.CreateAsync(
            request.Name,
            request.Description,
            request.Logo,
            request.RedirectUris,
            request.AllowedScopes,
            userId,
            request.IsPublic);

        await _clientService.SubmitAsync(client.Id);

        return new ResponseResult<ClientRegisteredResponse>(new ClientRegisteredResponse
        {
            ClientId = client.ClientId,
            ClientSecret = clientSecret,
            Status = ClientStatus.Pending.ToString(),
            IsPublic = client.IsPublic,
            Message = client.IsPublic
                ? "公开客户端注册成功，等待审核（无需 Client Secret）"
                : "客户端注册成功，等待审核"
        });
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<ResponseResult<IEnumerable<ClientResponse>>> GetMyClients()
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return ResponseResult<IEnumerable<ClientResponse>>.Unauthorized("未授权");
        }

        var clients = await _clientService.GetByUserIdAsync(userId.Value);
        return new ResponseResult<IEnumerable<ClientResponse>>(clients.Select(c => ToResponse(c)));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ResponseResult<ClientResponse>> Update(Guid id, [FromBody] UpdateClientRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return ResponseResult<ClientResponse>.NotFound("客户端未找到");
        }
        if (client.UserId != userId)
        {
            return ResponseResult<ClientResponse>.Forbidden("无权修改此客户端");
        }
        if (client.Status != ClientStatus.Draft)
        {
            return ResponseResult<ClientResponse>.BadRequest("仅草稿状态下可编辑，请先撤回");
        }

        client.Name = request.Name;
        client.Description = request.Description;
        client.Logo = request.Logo;
        client.RedirectUris = request.RedirectUris;
        client.AllowedScopes = request.AllowedScopes;
        await _clientService.UpdateAsync(client);

        return new ResponseResult<ClientResponse>(ToResponse(client)) { Message = "更新成功" };
    }

    [HttpPost("{id}/submit")]
    [Authorize]
    public async Task<ResponseResult<ClientResponse>> Submit(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return ResponseResult<ClientResponse>.NotFound("客户端未找到");
        }
        if (client.UserId != userId)
        {
            return ResponseResult<ClientResponse>.Forbidden("无权操作此客户端");
        }
        if (client.Status != ClientStatus.Draft)
        {
            return ResponseResult<ClientResponse>.BadRequest("仅草稿状态可提交审核");
        }

        await _clientService.SubmitAsync(id);
        client.Status = ClientStatus.Pending;
        return new ResponseResult<ClientResponse>(ToResponse(client)) { Message = "已提交审核" };
    }

    [HttpPost("{id}/withdraw")]
    [Authorize]
    public async Task<ResponseResult<ClientResponse>> Withdraw(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return ResponseResult<ClientResponse>.NotFound("客户端未找到");
        }
        if (client.UserId != userId)
        {
            return ResponseResult<ClientResponse>.Forbidden("无权操作此客户端");
        }
        if (client.Status == ClientStatus.Draft)
        {
            return ResponseResult<ClientResponse>.BadRequest("已是草稿状态");
        }

        await _clientService.WithdrawAsync(id);
        client.Status = ClientStatus.Draft;
        return new ResponseResult<ClientResponse>(ToResponse(client)) { Message = "已撤回，可重新编辑" };
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ResponseResult<object>> Delete(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return ResponseResult<object>.NotFound("客户端未找到");
        }

        var isAdmin = User.IsInRole("Admin");
        if (client.UserId != userId && !isAdmin)
        {
            return ResponseResult<object>.Forbidden("无权删除此客户端");
        }

        await _clientService.DeleteAsync(id);
        return ResponseResult<object>.Success("客户端已成功删除");
    }

    [HttpPost("{id}/approve")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ResponseResult<ClientResponse>> Approve(Guid id)
    {
        var adminId = _currentUserService.GetUserId();
        if (adminId == null)
        {
            return ResponseResult<ClientResponse>.Unauthorized("未授权");
        }

        await _clientService.ApproveAsync(id, adminId.Value);
        var client = await _clientService.GetByIdAsync(id);
        return new ResponseResult<ClientResponse>(ToResponse(client)) { Message = "客户端已批准" };
    }

    [HttpPost("{id}/reject")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public async Task<ResponseResult<ClientResponse>> Reject(Guid id)
    {
        var adminId = _currentUserService.GetUserId();
        if (adminId == null)
        {
            return ResponseResult<ClientResponse>.Unauthorized("未授权");
        }

        await _clientService.RejectAsync(id, adminId.Value);
        var client = await _clientService.GetByIdAsync(id);
        return new ResponseResult<ClientResponse>(ToResponse(client)) { Message = "客户端已拒绝" };
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
            IsPublic = client.IsPublic,
            Status = client.Status.ToString(),
            CreatedAt = client.CreatedAt
        };
    }
}