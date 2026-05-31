namespace OAuth.Contracts.ExternalAccount;

public class GithubCallbackResponse
{
    public bool Bound { get; set; }
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Provider { get; set; }
    public string? ProviderUserId { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}