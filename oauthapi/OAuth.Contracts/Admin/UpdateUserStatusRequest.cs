using OAuth.Domain.Entities;

namespace OAuth.Contracts.Admin;

public class UpdateUserStatusRequest
{
    public UserStatus Status { get; set; }
}
