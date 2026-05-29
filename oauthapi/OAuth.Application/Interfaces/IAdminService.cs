using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IAdminService
{
    Task<Admin?> GetByIdAsync(Guid id);
    Task<Admin?> GetByUsernameAsync(string username);
    Task<Admin> CreateAsync(string username, string password, AdminRole role = AdminRole.Operator);
    Task UpdateAsync(Admin admin);
    Task<bool> ValidatePasswordAsync(Admin admin, string password);
    Task<(bool Success, string Message)> ChangePasswordAsync(Guid adminId, string currentPassword, string newPassword);
    Task<List<User>> GetUsersAsync(int page, int pageSize, string? keyword);
    Task<int> GetTotalUsersCount();
    Task DeleteUserAsync(Guid userId);
}
