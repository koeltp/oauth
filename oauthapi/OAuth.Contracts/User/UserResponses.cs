namespace OAuth.Contracts.User;

public class UserInfoResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AuthorizationResponse
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? Logo { get; set; }
    public string? Scope { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BoundAccountResponse
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}