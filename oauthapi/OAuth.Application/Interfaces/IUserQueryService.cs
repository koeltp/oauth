using OAuth.Domain.Entities;
using Taipi.Core.RQRS;

namespace OAuth.Application.Interfaces;

public interface IUserQueryService
{
    Task<List<User>> GetUsersAsync(SearchPager<string?> pager);
    Task<int> GetTotalUsersCountAsync();
    Task DeleteUserAsync(Guid userId);
}