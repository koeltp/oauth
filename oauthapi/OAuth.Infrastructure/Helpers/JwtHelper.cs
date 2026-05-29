using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OAuth.Domain.Entities;

namespace OAuth.Infrastructure.Helpers;

public static class JwtHelper
{
    private static IConfiguration? _configuration;
    
    public static void Configure(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    private static string Issuer => _configuration?["Jwt:Issuer"] ?? "https://localhost:5168";
    private static string Audience => _configuration?["Jwt:Audience"] ?? "OAuth.Server";
    private static string SecretKey => _configuration?["Jwt:SecretKey"] ?? "your-256-bit-secret-key-for-jwt-signing-minimum-32-chars";
    private static int ExpirationMinutes => int.TryParse(_configuration?["Jwt:ExpirationMinutes"], out var m) ? m : 30;

    public static string GenerateUserToken(User user, string? role = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return GenerateToken(claims);
    }

    public static string GenerateAdminToken(Admin admin)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(ClaimTypes.Name, admin.Username),
            new Claim(ClaimTypes.Role, admin.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return GenerateToken(claims);
    }

    public static string GenerateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(ExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static int GetExpirationSeconds() => ExpirationMinutes * 60;
}
