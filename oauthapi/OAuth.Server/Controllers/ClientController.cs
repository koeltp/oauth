using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Client;
using OAuth.Domain.Entities;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/clients")]
[ApiVersion("1.0")]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clients = await _clientService.GetAllAsync();
        return Ok(clients.Select(c => new
        {
            c.Id,
            c.ClientId,
            c.Name,
            c.Logo,
            c.RedirectUris,
            c.AllowedScopes,
            c.Status,
            c.CreatedAt
        }));
    }

    [HttpGet("pending")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetPending()
    {
        var clients = await _clientService.GetPendingAsync();
        return Ok(clients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null)
        {
            return NotFound();
        }
        return Ok(client);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterClientRequest request)
    {
        var client = await _clientService.CreateAsync(
            request.Name,
            request.RedirectUris,
            request.AllowedScopes);

        return Ok(new
        {
            client_id = client.ClientId,
            message = "Client registered successfully, pending approval"
        });
    }

    [HttpPost("{id}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId) || !Guid.TryParse(adminId, out var adminGuidId))
        {
            return Unauthorized();
        }

        await _clientService.ApproveAsync(id, adminGuidId);
        return Ok(new { message = "Client approved" });
    }

    [HttpPost("{id}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId) || !Guid.TryParse(adminId, out var adminGuidId))
        {
            return Unauthorized();
        }

        await _clientService.RejectAsync(id, adminGuidId);
        return Ok(new { message = "Client rejected" });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _clientService.DeleteAsync(id);
        return Ok(new { message = "Client deleted successfully" });
    }
}
