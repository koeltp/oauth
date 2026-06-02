using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using OAuth.Application.Interfaces;
using OAuth.Infrastructure.Options;

namespace OAuth.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
            _logger.LogInformation("SMTP 连接成功: {Host}:{Port}", _options.Host, _options.Port);
            await client.AuthenticateAsync(_options.Username, _options.Password);
            _logger.LogInformation("SMTP 认证成功: {Username}", _options.Username);
            await client.SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送邮件失败: {To}, {Subject}", to, subject);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}