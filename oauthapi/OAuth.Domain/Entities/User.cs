namespace OAuth.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public bool TwoFactorEnabled { get; set; }
    /// <summary>
    /// 用于生成或验证两步验证一次性验证码的共享密钥。
    /// </summary>
    /// <remarks>包含敏感信息，应以受保护的方式存储并避免在日志或外部传输中泄露。若未配置两步验证，则为 null。</remarks>
    public string? TwoFactorSecret { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<VerificationCode> VerificationCodes { get; set; } = new List<VerificationCode>();
    public ICollection<UserExternalAccount> ExternalAccounts { get; set; } = new List<UserExternalAccount>();
    public ICollection<Authorization> Authorizations { get; set; } = new List<Authorization>();
}

public enum UserStatus
{
    Active,
    Inactive,
    Banned
}
