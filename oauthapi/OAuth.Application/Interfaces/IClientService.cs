using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IClientService
{
    Task<List<Client>> GetAllAsync();
    Task<List<Client>> GetPendingAsync();
    Task<List<Client>> GetByUserIdAsync(Guid userId);
    Task<Client?> GetByIdAsync(Guid id);
    Task<Client?> GetByClientIdAsync(string clientId);
    Task<(Client Client, string ClientSecret)> CreateAsync(string name, string? description, string redirectUris, string allowedScopes, Guid? userId = null);
    Task SubmitAsync(Guid id);
    Task ApproveAsync(Guid id, Guid reviewerId);
    Task RejectAsync(Guid id, Guid reviewerId);
    Task WithdrawAsync(Guid id);
    Task UpdateAsync(Client client);
    Task DeleteAsync(Guid id);
    Task<int> GetTotalClientsCount();
    Task<int> GetClientsCountByStatus(ClientStatus status);
}
