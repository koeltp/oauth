namespace OAuth.Infrastructure.Options;

public class TokenOptions
{
    public const string SectionName = "Token";
    
    public int RefreshTokenExpirationDays { get; set; } = 7;

    public int RefreshTokenExpirationSeconds => RefreshTokenExpirationDays * 24 * 60 * 60;
}