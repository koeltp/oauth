namespace OAuth.Contracts.Auth;

public class PasswordLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? TwoFaCode { get; set; }
}
