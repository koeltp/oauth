using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IOAuthAuthorizationService
{
    Task<List<Authorization>> GetByUserIdAsync(Guid userId);
    Task<Authorization?> GetByIdAsync(Guid id);
    Task<Authorization?> GetByCodeAsync(string code);
    Task<Authorization> CreateAsync(Guid userId, Guid clientId, string scope, string? redirectUri = null, string? codeChallenge = null, string? codeChallengeMethod = null);
    Task MarkCodeAsUsedAsync(string code);
    Task DeleteAsync(Guid id);
    Task<int> GetTotalAuthorizationsCount();
}