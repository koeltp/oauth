using Microsoft.EntityFrameworkCore;
using OAuth.Application.Interfaces;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Data;

namespace OAuth.Infrastructure.Services;

public class AuthorizationService : IOAuthAuthorizationService
{
    private readonly ApplicationDbContext _context;

    public AuthorizationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Authorization>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Authorizations
            .Include(a => a.Client)
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    public async Task<Authorization?> GetByIdAsync(Guid id)
    {
        return await _context.Authorizations.FindAsync(id);
    }

    public async Task<Authorization?> GetByCodeAsync(string code)
    {
        return await _context.Authorizations
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.Code == code);
    }

    public async Task<Authorization> CreateAsync(Guid userId, Guid clientId, string scope, string? redirectUri = null, string? codeChallenge = null, string? codeChallengeMethod = null)
    {
        var authorization = new Authorization
        {
            UserId = userId,
            ClientId = clientId,
            Code = GenerateCode(),
            Scope = scope,
            RedirectUri = redirectUri,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod,
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        await _context.Authorizations.AddAsync(authorization);
        await _context.SaveChangesAsync();

        return authorization;
    }

    public async Task MarkCodeAsUsedAsync(string code)
    {
        var authorization = await _context.Authorizations
            .FirstOrDefaultAsync(a => a.Code == code);

        if (authorization != null)
        {
            authorization.CodeUsed = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var authorization = await _context.Authorizations.FindAsync(id);
        if (authorization != null)
        {
            _context.Authorizations.Remove(authorization);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalAuthorizationsCount()
    {
        return await _context.Authorizations.CountAsync();
    }

    private string GenerateCode()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 32);
    }
}