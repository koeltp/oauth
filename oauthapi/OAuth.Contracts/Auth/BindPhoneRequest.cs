namespace OAuth.Contracts.Auth;

public class BindPhoneRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
