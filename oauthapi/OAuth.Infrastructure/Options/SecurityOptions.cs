namespace OAuth.Infrastructure.Options;

public class SecurityOptions
{
    public const string SectionName = "Security";
    
    public int VerificationCodeExpirationMinutes { get; set; } = 5;
    public int MaxVerificationCodeRetry { get; set; } = 5;
}