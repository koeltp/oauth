using OAuth.Contracts.Admin;
using OAuth.Domain.Entities;

namespace OAuth.Application.Mappers;

public static class UserMapper
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            Status = user.Status.ToString(),
            TwoFactorEnabled = user.TwoFactorEnabled,
            EmailVerified = user.EmailVerified,
            CreatedAt = user.CreatedAt
        };
    }
}