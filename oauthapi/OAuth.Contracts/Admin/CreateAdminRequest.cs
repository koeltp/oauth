using OAuth.Domain.Entities;

namespace OAuth.Contracts.Admin;

public class CreateAdminRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public AdminRole Role { get; set; } = AdminRole.Operator;
}
