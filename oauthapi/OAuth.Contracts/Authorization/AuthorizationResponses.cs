namespace OAuth.Contracts.Authorization;

public class AuthorizeResponse
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public string? UserId { get; set; }
}