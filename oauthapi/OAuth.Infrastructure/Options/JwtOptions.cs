namespace OAuth.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    public string SecretKey { get; set; } = string.Empty;
    
    public string Issuer { get; set; } = "https://localhost:5168";
    
    public string Audience { get; set; } = "OAuth.Server";
    
    public int ExpirationMinutes { get; set; } = 60;
}