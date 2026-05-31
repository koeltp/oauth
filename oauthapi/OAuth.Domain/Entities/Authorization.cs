namespace OAuth.Domain.Entities;

public class Authorization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string? RedirectUri { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CodeExpiresAt { get; set; }
    public bool CodeUsed { get; set; } = false;
}