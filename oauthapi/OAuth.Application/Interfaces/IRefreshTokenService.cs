using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IRefreshTokenService
{
    Task<RefreshToken> CreateAsync(Guid userId, Guid? clientId = null, string scope = "");
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeAsync(string token);
}
