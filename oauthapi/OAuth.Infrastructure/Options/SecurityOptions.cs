namespace OAuth.Infrastructure.Options;

public class SecurityOptions
{
    public const string SectionName = "Security";
    
    /// <summary>
    /// 验证码有效期：5分钟
    /// </summary>
    public int VerificationCodeExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// 最大验证码发送次数
    /// </summary>
    public int MaxVerificationCodeRetry { get; set; } = 5;
}