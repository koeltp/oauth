using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IAdminRefreshTokenService
{
    Task<AdminRefreshToken> CreateAsync(Guid adminId);
    Task<AdminRefreshToken?> GetByTokenAsync(string token);
    Task RevokeAsync(string token);
    Task<bool> IsValidAsync(string token);
}