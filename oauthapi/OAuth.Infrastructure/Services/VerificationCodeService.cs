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
    private const int CodeLength = 6;
    private static readonly Random _random = new();

    public VerificationCodeService(IDistributedCache cache, IOptions<SecurityOptions> securityOptions)
    {
        _cache = cache;
        _securityOptions = securityOptions.Value;
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

        return expiresInMinutes * 60;
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