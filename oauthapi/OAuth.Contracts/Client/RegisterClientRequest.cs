namespace OAuth.Contracts.Client;

public class RegisterClientRequest
{
    public string Name { get; set; } = string.Empty;
    public string RedirectUris { get; set; } = string.Empty;
    public string AllowedScopes { get; set; } = "openid profile email";
}
