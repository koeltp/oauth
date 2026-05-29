using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IExternalAccountService
{
    Task<UserExternalAccount?> GetByProviderAsync(ExternalProvider provider, string providerUserId);
    Task<UserExternalAccount> BindAsync(Guid userId, ExternalProvider provider, string providerUserId, string? accessToken = null, string? refreshToken = null, DateTime? tokenExpiresAt = null);
    Task UnbindAsync(Guid userId, ExternalProvider provider);
}
