using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IUserExternalAccountService
{
    Task<List<UserExternalAccount>> GetByUserIdAsync(Guid userId);
    Task<UserExternalAccount?> GetByIdAsync(Guid id);
    Task<UserExternalAccount?> GetByProviderAndUserIdAsync(string provider, string providerUserId);
    Task CreateAsync(UserExternalAccount account);
    Task DeleteAsync(Guid id);
}
