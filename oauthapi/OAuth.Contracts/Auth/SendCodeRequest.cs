using OAuth.Domain.Entities;

namespace OAuth.Contracts.Auth;

public class SendCodeRequest
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public VerificationCodePurpose Purpose { get; set; }
}
