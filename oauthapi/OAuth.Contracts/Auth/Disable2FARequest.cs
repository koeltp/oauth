namespace OAuth.Contracts.Auth;

public class Disable2FARequest
{
    public string Code { get; set; } = string.Empty;
}
