using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IRefreshTokenService
{
    Task<RefreshToken> CreateAsync(Guid userId, Guid clientId, string scope);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeAsync(string token);
    Task RevokeByUserIdAsync(Guid userId);
    Task RevokeByClientIdAsync(Guid clientId);
    Task<bool> IsValidAsync(string token);
}
