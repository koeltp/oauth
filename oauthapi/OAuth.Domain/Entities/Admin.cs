namespace OAuth.Domain.Entities;

public class Admin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public AdminRole Role { get; set; } = AdminRole.Operator;
    public AdminStatus Status { get; set; } = AdminStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Client> ReviewedClients { get; set; } = new List<Client>();
    public ICollection<AdminRefreshToken> RefreshTokens { get; set; } = new List<AdminRefreshToken>();
}

public enum AdminRole
{
    SuperAdmin,
    Admin,
    Operator
}

public enum AdminStatus
{
    Active,
    Inactive
}
