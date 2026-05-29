using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;

namespace OAuth.Server.Controllers;

[Route("connect")]
public class UserInfoController : Controller
{
    [HttpGet("userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult UserInfo()
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
            ["name"] = User.FindFirstValue(ClaimTypes.Name) ?? "",
            ["email"] = User.FindFirstValue(ClaimTypes.Email) ?? "",
            ["email_verified"] = true
        };

        return Ok(claims);
    }
}
