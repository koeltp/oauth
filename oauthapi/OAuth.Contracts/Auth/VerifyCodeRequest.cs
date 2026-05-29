using OAuth.Domain.Entities;

namespace OAuth.Contracts.Auth;

public class VerifyCodeRequest
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Code { get; set; } = string.Empty;
    public VerificationCodePurpose Purpose { get; set; }
}
