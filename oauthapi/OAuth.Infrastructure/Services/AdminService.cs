using Microsoft.EntityFrameworkCore;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Admin;
using Taipi.Core.Linq;
using Taipi.Core.RQRS;
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

    public async Task<PagerResponse<AdminDto>> GetListAsync(SearchPager<string?> pager)
    {
        var query = _context.Admins.AsQueryable()
            .WhereIf(!string.IsNullOrWhiteSpace(pager.Condition), a =>
                a.Username.Contains(pager.Condition!) ||
                (a.Email != null && a.Email.Contains(pager.Condition!)));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Page(pager)
            .Select(a => new AdminDto
            {
                Id = a.Id,
                Username = a.Username,
                Email = a.Email,
                Role = a.Role.ToString(),
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt,
                LastLoginAt = a.LastLoginAt
            })
            .ToListAsync();

        return new PagerResponse<AdminDto>
        {
            Items = items,
            TotalCount = total,
            PageIndex = pager.PageIndex,
            PageSize = pager.PageSize
        };
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
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            Status = AdminStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        return admin;
    }

    public async Task UpdateAsync(Admin admin)
    {
        _context.Admins.Update(admin);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var admin = await _context.Admins.FindAsync(id);
        if (admin != null)
        {
            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ValidatePasswordAsync(Admin admin, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash);
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid adminId, string currentPassword, string newPassword)
    {
        var admin = await _context.Admins.FindAsync(adminId);
        if (admin == null)
            return (false, "管理员不存在");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, admin.PasswordHash))
            return (false, "当前密码错误");

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        admin.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, "密码修改成功");
    }
}