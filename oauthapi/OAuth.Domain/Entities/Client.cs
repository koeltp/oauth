namespace OAuth.Domain.Entities;

public class Client
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecretHash { get; set; } = string.Empty;
    public string ClientSecretEncrypted { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Description { get; set; }
    public string RedirectUris { get; set; } = string.Empty;
    public string AllowedScopes { get; set; } = string.Empty;
    public ClientStatus Status { get; set; } = ClientStatus.Draft;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid? ReviewerId { get; set; }
    public Admin? Reviewer { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Authorization> Authorizations { get; set; } = new List<Authorization>();
}

public enum ClientStatus
{
    Draft,
    Pending,
    Approved,
    Rejected
}
