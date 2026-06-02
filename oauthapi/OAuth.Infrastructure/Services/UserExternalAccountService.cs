using Microsoft.EntityFrameworkCore;
using OAuth.Application.Interfaces;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Data;

namespace OAuth.Infrastructure.Services;

public class UserExternalAccountService : IUserExternalAccountService
{
    private readonly ApplicationDbContext _context;

    public UserExternalAccountService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserExternalAccount>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserExternalAccounts
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserExternalAccount?> GetByIdAsync(Guid id)
    {
        return await _context.UserExternalAccounts.FindAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var account = await _context.UserExternalAccounts.FindAsync(id);
        if (account != null)
        {
            _context.UserExternalAccounts.Remove(account);
            await _context.SaveChangesAsync();
        }
    }
}
