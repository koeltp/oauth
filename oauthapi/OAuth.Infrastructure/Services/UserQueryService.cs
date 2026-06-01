using Microsoft.EntityFrameworkCore;
using OAuth.Application.Interfaces;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Data;
using Taipi.Core.Linq;
using Taipi.Core.RQRS;

namespace OAuth.Infrastructure.Services;

public class UserQueryService : IUserQueryService
{
    private readonly ApplicationDbContext _context;

    public UserQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetUsersAsync(SearchPager<string?> pager)
    {
        var query = _context.Users.AsQueryable()
            .WhereIf(!string.IsNullOrEmpty(pager.Condition), u =>
                u.Username.Contains(pager.Condition!) || u.Email.Contains(pager.Condition!));

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Page(pager)
            .ToListAsync();
    }

    public async Task<int> GetTotalUsersCountAsync()
    {
        return await _context.Users.CountAsync();
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}