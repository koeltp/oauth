using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User> CreateAsync(User user, string password);
    Task<bool> ValidatePasswordAsync(User user, string password);
    Task UpdateAsync(User user);
    Task<int> GetTotalUsersCount();
    Task<List<User>> GetRecentUsersAsync(int count);
}
