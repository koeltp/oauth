using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAuth.Application.Interfaces;
using OAuth.Infrastructure.Options;

namespace OAuth.Infrastructure.Services;

public class SmsService : ISmsService
{
    private readonly SmsOptions _options;
    private readonly ILogger<SmsService> _logger;

    public SmsService(IOptions<SmsOptions> options, ILogger<SmsService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(string phoneNumber, string code)
    {
        if (string.IsNullOrEmpty(_options.AccessKey) || _options.AccessKey == "your-access-key")
        {
            _logger.LogInformation("短信服务未配置，验证码 {Code} 发送到 {Phone}", code, phoneNumber);
            return;
        }

        await SendAliyunSmsAsync(phoneNumber, code);
    }

    private async Task SendAliyunSmsAsync(string phoneNumber, string code)
    {
        using var httpClient = new HttpClient();
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var parameters = new Dictionary<string, string>
        {
            { "AccessKeyId", _options.AccessKey },
            { "Action", "SendSms" },
            { "Format", "JSON" },
            { "RegionId", "cn-hangzhou" },
            { "SignatureMethod", "HMAC-SHA1" },
            { "SignatureNonce", Guid.NewGuid().ToString("N") },
            { "SignatureVersion", "1.0" },
            { "Timestamp", timestamp },
            { "Version", "2017-05-25" },
            { "PhoneNumbers", phoneNumber },
            { "SignName", _options.SignName },
            { "TemplateParam", $"{{\"code\":\"{code}\"}}" }
        };

        if (!string.IsNullOrEmpty(_options.TemplateCode))
        {
            parameters["TemplateCode"] = _options.TemplateCode;
        }

        var sortedParams = parameters.OrderBy(p => p.Key, StringComparer.Ordinal).ToList();
        var queryString = string.Join("&", sortedParams.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var stringToSign = $"POST&%2F&{Uri.EscapeDataString(queryString)}";
        var signature = ComputeHmacSha1(stringToSign, $"{_options.AccessSecret}&");

        var requestUrl = $"https://dysmsapi.aliyuncs.com/?{queryString}&Signature={Uri.EscapeDataString(signature)}";

        var response = await httpClient.PostAsync(requestUrl, null);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("发送短信失败: {StatusCode}, {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException("发送短信失败");
        }

        _logger.LogInformation("短信发送成功: {Phone}, {Code}", phoneNumber, code);
    }

    private static string ComputeHmacSha1(string text, string key)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA1(
            System.Text.Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text));
        return Convert.ToBase64String(hash);
    }
}