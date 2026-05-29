using Microsoft.EntityFrameworkCore;
using OAuth.Application.Interfaces;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Data;

namespace OAuth.Infrastructure.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _dbContext;

    public RefreshTokenService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshToken> CreateAsync(Guid userId, Guid clientId, string scope)
    {
        var token = new RefreshToken
        {
            UserId = userId,
            ClientId = clientId,
            Token = GenerateToken(),
            Scope = scope,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _dbContext.RefreshTokens.AddAsync(token);
        await _dbContext.SaveChangesAsync();

        return token;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbContext.RefreshTokens
            .Include(t => t.User)
            .Include(t => t.Client)
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    public async Task RevokeAsync(string token)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (refreshToken != null)
        {
            refreshToken.Revoked = true;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task RevokeByUserIdAsync(Guid userId)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Revoked = true;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task RevokeByClientIdAsync(Guid clientId)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(t => t.ClientId == clientId)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Revoked = true;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> IsValidAsync(string token)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        return refreshToken != null && 
               !refreshToken.Revoked && 
               refreshToken.ExpiresAt > DateTime.UtcNow;
    }

    private string GenerateToken()
    {
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }
}
