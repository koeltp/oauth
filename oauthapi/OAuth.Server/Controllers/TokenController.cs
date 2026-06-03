using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace OAuth.Server.Controllers;

[Route("connect")]
public class TokenController : Controller
{
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IClientService _clientService;
    private readonly IUserService _userService;
    private readonly IOAuthAuthorizationService _authorizationService;

    public TokenController(
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IClientService clientService,
        IUserService userService,
        IOAuthAuthorizationService authorizationService)
    {
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _clientService = clientService;
        _userService = userService;
        _authorizationService = authorizationService;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> Token()
    {
        var grantType = Request.Form["grant_type"].ToString();

        if (string.Equals(grantType, "client_credentials", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleClientCredentialsAsync();
        }

        if (string.Equals(grantType, "authorization_code", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleAuthorizationCodeAsync();
        }

        if (string.Equals(grantType, "refresh_token", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleRefreshTokenAsync();
        }

        return BadRequest(new { error = "unsupported_grant_type" });
    }

    private async Task<IActionResult> HandleClientCredentialsAsync()
    {
        var clientId = Request.Form["client_id"].ToString();
        var clientSecret = Request.Form["client_secret"].ToString();

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return BadRequest(new { error = "invalid_client" });
        }

        var client = await _clientService.GetByClientIdAsync(clientId);
        if (client == null || !BCrypt.Net.BCrypt.Verify(clientSecret, client.ClientSecretHash))
        {
            return BadRequest(new { error = "invalid_client" });
        }

        var requestedScope = Request.Form["scope"].ToString();
        var validatedScope = ValidateRequestedScopes(requestedScope, client.AllowedScopes);
        if (validatedScope == null)
        {
            return BadRequest(new { error = "invalid_scope", error_description = "请求的 scope 不在客户端允许范围内" });
        }

        var accessToken = _jwtService.GenerateClientToken(client.Id, client.ClientId);

        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = _jwtService.GetExpirationSeconds(),
            scope = validatedScope
        });
    }

    private async Task<IActionResult> HandleAuthorizationCodeAsync()
    {
        var code = Request.Form["code"].ToString();
        var clientId = Request.Form["client_id"].ToString();
        var clientSecret = Request.Form["client_secret"].ToString();
        var redirectUri = Request.Form["redirect_uri"].ToString();
        var codeVerifier = Request.Form["code_verifier"].ToString();

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { error = "invalid_grant" });
        }

        if (string.IsNullOrEmpty(clientId))
        {
            return BadRequest(new { error = "invalid_client" });
        }

        var client = await _clientService.GetByClientIdAsync(clientId);
        if (client == null)
        {
            return BadRequest(new { error = "invalid_client" });
        }

        if (!client.IsPublic)
        {
            if (string.IsNullOrEmpty(clientSecret) || !BCrypt.Net.BCrypt.Verify(clientSecret, client.ClientSecretHash))
            {
                return BadRequest(new { error = "invalid_client" });
            }
        }

        var authorization = await _authorizationService.GetByCodeAsync(code);
        if (authorization == null || authorization.ClientId != client.Id)
        {
            return BadRequest(new { error = "invalid_grant" });
        }

        if (authorization.CodeUsed)
        {
            return BadRequest(new { error = "invalid_grant", error_description = "Authorization code has already been used" });
        }

        if (authorization.CodeExpiresAt.HasValue && authorization.CodeExpiresAt.Value <= DateTime.UtcNow)
        {
            return BadRequest(new { error = "invalid_grant", error_description = "Authorization code has expired" });
        }

        if (!string.IsNullOrEmpty(authorization.RedirectUri) &&
            !string.Equals(authorization.RedirectUri, redirectUri, StringComparison.Ordinal))
        {
            return BadRequest(new { error = "invalid_grant", error_description = "redirect_uri mismatch" });
        }

        if (!string.IsNullOrEmpty(authorization.CodeChallenge))
        {
            if (string.IsNullOrEmpty(codeVerifier))
            {
                return BadRequest(new { error = "invalid_grant", error_description = "code_verifier is required" });
            }

            var expectedChallenge = authorization.CodeChallengeMethod switch
            {
                "S256" => ComputeS256CodeChallenge(codeVerifier),
                "plain" => codeVerifier,
                _ => null
            };

            if (expectedChallenge == null || !string.Equals(expectedChallenge, authorization.CodeChallenge, StringComparison.Ordinal))
            {
                return BadRequest(new { error = "invalid_grant", error_description = "code_verifier mismatch" });
            }
        }

        var validatedScope = ValidateRequestedScopes(authorization.Scope, client.AllowedScopes);
        if (validatedScope == null)
        {
            return BadRequest(new { error = "invalid_scope", error_description = "请求的 scope 不在客户端允许范围内" });
        }

        await _authorizationService.MarkCodeAsUsedAsync(code);

        var user = await _userService.GetByIdAsync(authorization.UserId);
        if (user == null)
        {
            return BadRequest(new { error = "invalid_grant" });
        }

        var accessToken = _jwtService.GenerateUserToken(user);
        var refreshToken = await _refreshTokenService.CreateAsync(user.Id, client.Id, validatedScope);

        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = _jwtService.GetExpirationSeconds(),
            refresh_token = refreshToken.Token,
            scope = validatedScope
        });
    }

    private async Task<IActionResult> HandleRefreshTokenAsync()
    {
        var refreshToken = Request.Form["refresh_token"].ToString();
        var clientId = Request.Form["client_id"].ToString();
        var clientSecret = Request.Form["client_secret"].ToString();

        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { error = "invalid_grant" });
        }

        var token = await _refreshTokenService.GetByTokenAsync(refreshToken);
        if (token == null || token.Revoked || token.ExpiresAt <= DateTime.UtcNow)
        {
            return BadRequest(new { error = "invalid_grant" });
        }

        if (!string.IsNullOrEmpty(clientId) || !string.IsNullOrEmpty(clientSecret))
        {
            var client = await _clientService.GetByClientIdAsync(clientId);
            if (client == null || client.Id != token.ClientId)
            {
                return BadRequest(new { error = "invalid_client" });
            }

            if (!client.IsPublic && (string.IsNullOrEmpty(clientSecret) || !BCrypt.Net.BCrypt.Verify(clientSecret, client.ClientSecretHash)))
            {
                return BadRequest(new { error = "invalid_client" });
            }
        }

        await _refreshTokenService.RevokeAsync(refreshToken);

        var newRefreshToken = await _refreshTokenService.CreateAsync(token.UserId, token.ClientId, token.Scope);

        var user = await _userService.GetByIdAsync(token.UserId);
        if (user == null)
        {
            return BadRequest(new { error = "invalid_grant" });
        }

        var accessToken = _jwtService.GenerateUserToken(user);

        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = _jwtService.GetExpirationSeconds(),
            refresh_token = newRefreshToken.Token,
            scope = token.Scope
        });
    }

    private static string ComputeS256CodeChallenge(string codeVerifier)
    {
        var bytes = Encoding.ASCII.GetBytes(codeVerifier);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string? ValidateRequestedScopes(string? requestedScope, string allowedScopes)
    {
        if (string.IsNullOrWhiteSpace(requestedScope))
        {
            return string.Empty;
        }

        var allowed = allowedScopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();

        var requested = requestedScope
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var validScopes = requested.Where(s => allowed.Contains(s)).ToArray();

        if (validScopes.Length != requested.Length)
        {
            return null;
        }

        return string.Join(" ", validScopes);
    }
}