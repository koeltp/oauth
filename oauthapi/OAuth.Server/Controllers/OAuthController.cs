using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Authorization;
using Taipi.Core.RQRS;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}")]
[ApiVersion("1.0")]
public class OAuthController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly ICurrentUserService _currentUserService;

    public OAuthController(IClientService clientService, ICurrentUserService currentUserService)
    {
        _clientService = clientService;
        _currentUserService = currentUserService;
    }

    [HttpGet("authorize")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<ResponseResult<AuthorizeResponse>> Authorize()
    {
        var clientId = Request.Query["client_id"].ToString();
        var scope = Request.Query["scope"].ToString();

        if (string.IsNullOrEmpty(clientId))
        {
            return ResponseResult<AuthorizeResponse>.BadRequest("缺少客户端标识");
        }

        var client = await _clientService.GetByClientIdAsync(clientId);
        if (client == null)
        {
            return ResponseResult<AuthorizeResponse>.BadRequest("无效的客户端");
        }

        var userIdValue = _currentUserService.GetUserId()?.ToString();

        return new ResponseResult<AuthorizeResponse>(new AuthorizeResponse
        {
            ClientId = client.ClientId,
            ClientName = client.Name,
            Scopes = scope,
            UserId = userIdValue
        });
    }
}