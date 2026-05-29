using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IClientService
{
    Task<List<Client>> GetAllAsync();
    Task<List<Client>> GetPendingAsync();
    Task<Client?> GetByIdAsync(Guid id);
    Task<Client?> GetByClientIdAsync(string clientId);
    Task<Client> CreateAsync(string name, string redirectUris, string allowedScopes);
    Task ApproveAsync(Guid id, Guid reviewerId);
    Task RejectAsync(Guid id, Guid reviewerId);
    Task UpdateAsync(Client client);
    Task DeleteAsync(Guid id);
    Task<int> GetTotalClientsCount();
    Task<int> GetClientsCountByStatus(ClientStatus status);
}
