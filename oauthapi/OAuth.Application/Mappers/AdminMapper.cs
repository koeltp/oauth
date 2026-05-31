using OAuth.Contracts.Admin;
using OAuth.Domain.Entities;

namespace OAuth.Application.Mappers;

public static class AdminMapper
{
    public static AdminDto ToDto(this Admin admin)
    {
        return new AdminDto
        {
            Id = admin.Id,
            Username = admin.Username,
            Email = admin.Email,
            Role = admin.Role.ToString(),
            Status = admin.Status.ToString(),
            CreatedAt = admin.CreatedAt,
            LastLoginAt = admin.LastLoginAt
        };
    }

    public static ProfileResponse ToProfileResponse(this Admin admin)
    {
        return new ProfileResponse
        {
            Id = admin.Id,
            Username = admin.Username,
            Email = admin.Email,
            AvatarUrl = admin.AvatarUrl,
            Role = admin.Role.ToString(),
            CreatedAt = admin.CreatedAt,
            LastLoginAt = admin.LastLoginAt
        };
    }
}