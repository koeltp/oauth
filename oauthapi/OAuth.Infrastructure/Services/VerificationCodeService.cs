using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Auth;
using OAuth.Infrastructure.Options;
using System.Text.Json;

namespace OAuth.Infrastructure.Services;

public class VerificationCodeService : IVerificationCodeService
{
    private readonly IDistributedCache _cache;
    private readonly SecurityOptions _securityOptions;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private const int CodeLength = 6;
    private static readonly Random _random = new();

    public VerificationCodeService(IDistributedCache cache, IOptions<SecurityOptions> securityOptions, IEmailService emailService, ISmsService smsService)
    {
        _cache = cache;
        _securityOptions = securityOptions.Value;
        _emailService = emailService;
        _smsService = smsService;
    }

    public async Task<int> CreateAsync(string identifier, VerificationCodeType type, VerificationCodePurpose purpose)
    {
        var code = new string(Enumerable.Range(0, CodeLength).Select(_ => _random.Next(0, 10).ToString()[0]).ToArray());
        var cacheKey = BuildCacheKey(identifier, purpose);
        var expiresInMinutes = _securityOptions.VerificationCodeExpirationMinutes;

        var value = JsonSerializer.SerializeToUtf8Bytes(new VerificationCodeValue
        {
            Code = code,
            RetryCount = 0
        });

        await _cache.SetAsync(cacheKey, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiresInMinutes)
        });

        await SendCodeAsync(identifier, type, purpose, code);

        return expiresInMinutes * 60;
    }

    private async Task SendCodeAsync(string identifier, VerificationCodeType type, VerificationCodePurpose purpose, string code)
    {
        var subject = purpose switch
        {
            VerificationCodePurpose.Login => "登录验证码",
            VerificationCodePurpose.Register => "注册验证码",
            VerificationCodePurpose.ResetPassword => "重置密码验证码",
            VerificationCodePurpose.BindPhone => "绑定手机验证码",
            VerificationCodePurpose.BindEmail => "绑定邮箱验证码",
            VerificationCodePurpose.ChangeEmail => "修改邮箱验证码",
            VerificationCodePurpose.ChangePhone => "修改手机验证码",
            _ => "验证码"
        };

        if (type == VerificationCodeType.Email)
        {
            await _emailService.SendAsync(identifier, subject, $"您的验证码是：<strong>{code}</strong>，有效期 {_securityOptions.VerificationCodeExpirationMinutes} 分钟。");
        }
        else if (type == VerificationCodeType.Sms)
        {
            await _smsService.SendVerificationCodeAsync(identifier, code);
        }
    }

    public async Task<bool> ValidateAsync(string identifier, string code, VerificationCodePurpose purpose)
    {
        var cacheKey = BuildCacheKey(identifier, purpose);
        var cached = await _cache.GetAsync(cacheKey);

        if (cached == null)
        {
            return false;
        }

        var value = JsonSerializer.Deserialize<VerificationCodeValue>(cached);
        if (value == null)
        {
            return false;
        }

        if (value.Code != code)
        {
            value.RetryCount++;

            if (value.RetryCount >= _securityOptions.MaxVerificationCodeRetry)
            {
                await _cache.RemoveAsync(cacheKey);
                return false;
            }

            var updatedBytes = JsonSerializer.SerializeToUtf8Bytes(value);
            await _cache.SetAsync(cacheKey, updatedBytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_securityOptions.VerificationCodeExpirationMinutes)
            });

            return false;
        }

        await _cache.RemoveAsync(cacheKey);
        return true;
    }

    private static string BuildCacheKey(string identifier, VerificationCodePurpose purpose)
    {
        return $"verification_code:{purpose:D}:{identifier}";
    }

    private class VerificationCodeValue
    {
        public string Code { get; set; } = string.Empty;
        public int RetryCount { get; set; }
    }
}