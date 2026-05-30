using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IUserQueryService
{
    Task<List<User>> GetUsersAsync(int page, int pageSize, string? keyword);
    Task<int> GetTotalUsersCountAsync();
    Task DeleteUserAsync(Guid userId);
}