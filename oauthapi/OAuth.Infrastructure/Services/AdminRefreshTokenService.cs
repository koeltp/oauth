using Microsoft.EntityFrameworkCore;
using OAuth.Application.Interfaces;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Data;

namespace OAuth.Infrastructure.Services;

public class AdminRefreshTokenService : IAdminRefreshTokenService
{
    private readonly ApplicationDbContext _dbContext;

    public AdminRefreshTokenService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminRefreshToken> CreateAsync(Guid adminId)
    {
        var token = new AdminRefreshToken
        {
            AdminId = adminId,
            Token = GenerateToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _dbContext.AdminRefreshTokens.AddAsync(token);
        await _dbContext.SaveChangesAsync();

        return token;
    }

    public async Task<AdminRefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbContext.AdminRefreshTokens
            .Include(t => t.Admin)
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    public async Task RevokeAsync(string token)
    {
        var refreshToken = await _dbContext.AdminRefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (refreshToken != null)
        {
            refreshToken.Revoked = true;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> IsValidAsync(string token)
    {
        var refreshToken = await _dbContext.AdminRefreshTokens
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