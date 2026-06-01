namespace OAuth.Infrastructure.Options;

public class WechatOptions
{
    public const string SectionName = "Wechat";

    public string AppId { get; set; } = string.Empty;

    public string AppSecret { get; set; } = string.Empty;

    public string CallbackUrl { get; set; } = "https://localhost:5001/api/external/wechat/callback";
}