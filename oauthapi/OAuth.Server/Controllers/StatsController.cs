using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Domain.Entities;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class StatsController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IClientService _clientService;
    private readonly IOAuthAuthorizationService _authorizationService;

    public StatsController(
        IUserService userService,
        IClientService clientService,
        IOAuthAuthorizationService authorizationService)
    {
        _userService = userService;
        _clientService = clientService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetStats()
    {
        var stats = new
        {
            TotalUsers = await _userService.GetTotalUsersCount(),
            TotalClients = await _clientService.GetTotalClientsCount(),
            ActiveClients = await _clientService.GetClientsCountByStatus(ClientStatus.Approved),
            TotalAuthorizations = await _authorizationService.GetTotalAuthorizationsCount()
        };

        return Ok(stats);
    }
}