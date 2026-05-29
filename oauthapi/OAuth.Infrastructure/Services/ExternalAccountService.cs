using Microsoft.EntityFrameworkCore;
using OAuth.Application.Interfaces;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Data;

namespace OAuth.Infrastructure.Services;

public class ExternalAccountService : IExternalAccountService
{
    private readonly ApplicationDbContext _context;

    public ExternalAccountService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserExternalAccount?> GetByProviderAsync(ExternalProvider provider, string providerUserId)
    {
        return await _context.UserExternalAccounts
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Provider == provider && e.ProviderUserId == providerUserId);
    }

    public async Task<UserExternalAccount> BindAsync(Guid userId, ExternalProvider provider, string providerUserId, string? accessToken = null, string? refreshToken = null, DateTime? tokenExpiresAt = null)
    {
        var existing = await GetByProviderAsync(provider, providerUserId);
        if (existing != null)
        {
            existing.AccessToken = accessToken;
            existing.RefreshToken = refreshToken;
            existing.TokenExpiresAt = tokenExpiresAt;
            await _context.SaveChangesAsync();
            return existing;
        }

        var account = new UserExternalAccount
        {
            UserId = userId,
            Provider = provider,
            ProviderUserId = providerUserId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiresAt = tokenExpiresAt
        };

        _context.UserExternalAccounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task UnbindAsync(Guid userId, ExternalProvider provider)
    {
        var account = await _context.UserExternalAccounts
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == provider);

        if (account != null)
        {
            _context.UserExternalAccounts.Remove(account);
            await _context.SaveChangesAsync();
        }
    }
}
