using OAuth.Contracts.Admin;
using OAuth.Contracts.Common;
using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IAdminService
{
    Task<PagedResultResponse<AdminDto>> GetAllAsync(int page, int pageSize, string? keyword);
    Task<Admin?> GetByIdAsync(Guid id);
    Task<Admin?> GetByUsernameAsync(string username);
    Task<Admin> CreateAsync(string username, string password, AdminRole role = AdminRole.Operator);
    Task UpdateAsync(Admin admin);
    Task DeleteAsync(Guid id);
    Task<bool> ValidatePasswordAsync(Admin admin, string password);
    Task<(bool Success, string Message)> ChangePasswordAsync(Guid adminId, string currentPassword, string newPassword);
}
