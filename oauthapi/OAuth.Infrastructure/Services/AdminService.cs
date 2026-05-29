using Microsoft.EntityFrameworkCore;
using OAuth.Application.Interfaces;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Data;

namespace OAuth.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;

    public AdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Admin?> GetByIdAsync(Guid id)
    {
        return await _context.Admins.FindAsync(id);
    }

    public async Task<Admin?> GetByUsernameAsync(string username)
    {
        return await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
    }

    public async Task<Admin> CreateAsync(string username, string password, AdminRole role = AdminRole.Operator)
    {
        var admin = new Admin
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();
        return admin;
    }

    public Task<bool> ValidatePasswordAsync(Admin admin, string password)
    {
        return Task.FromResult(BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash));
    }

    public async Task UpdateAsync(Admin admin)
    {
        _context.Admins.Update(admin);
        await _context.SaveChangesAsync();
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid adminId, string currentPassword, string newPassword)
    {
        var admin = await _context.Admins.FindAsync(adminId);
        if (admin == null)
        {
            return (false, "管理员不存在");
        }

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, admin.PasswordHash))
        {
            return (false, "当前密码不正确");
        }

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        admin.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, "密码修改成功");
    }

    public async Task<List<User>> GetUsersAsync(int page, int pageSize, string? keyword)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(u => u.Username.Contains(keyword) || u.Email.Contains(keyword));
        }

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalUsersCount()
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
