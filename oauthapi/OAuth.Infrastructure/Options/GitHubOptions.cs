namespace OAuth.Infrastructure.Options;

public class GitHubOptions
{
    public const string SectionName = "Github";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string CallbackUrl { get; set; } = "https://localhost:5001/api/1.0/external/github/callback";

    public string FrontendCallbackUrl { get; set; } = "https://localhost:5173/auth/github/callback";
}