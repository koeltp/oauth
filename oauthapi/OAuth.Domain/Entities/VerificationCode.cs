namespace OAuth.Domain.Entities;

public class VerificationCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Code { get; set; } = string.Empty;
    public VerificationCodeType Type { get; set; }
    public VerificationCodePurpose Purpose { get; set; }
    public int RetryCount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum VerificationCodeType
{
    Email,
    Sms
}

public enum VerificationCodePurpose
{
    Login,
    Register,
    ResetPassword,
    BindPhone,
    BindEmail,
    ChangeEmail,
    ChangePhone
}
