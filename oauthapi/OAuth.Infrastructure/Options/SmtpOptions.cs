namespace OAuth.Infrastructure.Options;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

public class SmsOptions
{
    public const string SectionName = "Sms";

    public string Provider { get; set; } = "aliyun";
    public string AccessKey { get; set; } = string.Empty;
    public string AccessSecret { get; set; } = string.Empty;
    public string SignName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
}