using OAuth.Contracts.Admin;
using Taipi.Core.RQRS;
using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IAdminService
{
    Task<PagerResponse<AdminDto>> GetListAsync(SearchPager<string?> pager);
    Task<Admin?> GetByIdAsync(Guid id);
    Task<Admin?> GetByUsernameAsync(string username);
    Task<Admin> CreateAsync(string username, string password, AdminRole role = AdminRole.Operator);
    Task UpdateAsync(Admin admin);
    Task DeleteAsync(Guid id);
    Task<bool> ValidatePasswordAsync(Admin admin, string password);
    Task<(bool Success, string Message)> ChangePasswordAsync(Guid adminId, string currentPassword, string newPassword);
}