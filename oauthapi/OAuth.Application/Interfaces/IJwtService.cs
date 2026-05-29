using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IJwtService
{
    string GenerateUserToken(User user, string? role = null);
    
    string GenerateAdminToken(Admin admin);
    
    int GetExpirationSeconds();
}