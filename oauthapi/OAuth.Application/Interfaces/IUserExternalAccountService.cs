using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IUserExternalAccountService
{
    Task<List<UserExternalAccount>> GetByUserIdAsync(Guid userId);
    Task<UserExternalAccount?> GetByIdAsync(Guid id);
    Task DeleteAsync(Guid id);
}
