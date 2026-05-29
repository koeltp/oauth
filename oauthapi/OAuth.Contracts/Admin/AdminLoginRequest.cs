using OAuth.Domain.Entities;

namespace OAuth.Contracts.Admin;

public class AdminLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
