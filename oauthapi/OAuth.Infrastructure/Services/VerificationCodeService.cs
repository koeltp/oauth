using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OAuth.Application.Interfaces;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Data;
using OAuth.Infrastructure.Options;

namespace OAuth.Infrastructure.Services;

public class VerificationCodeService : IVerificationCodeService
{
    private readonly ApplicationDbContext _context;
    private readonly SecurityOptions _securityOptions;
    private const int CodeLength = 6;
    private static readonly Random _random = new();

    public VerificationCodeService(ApplicationDbContext context, IOptions<SecurityOptions> securityOptions)
    {
        _context = context;
        _securityOptions = securityOptions.Value;
    }

    public async Task<VerificationCode> CreateAsync(string? email, string? phone, VerificationCodePurpose purpose, VerificationCodeType type = VerificationCodeType.Email)
    {
        var code = new string(Enumerable.Range(0, CodeLength).Select(_ => _random.Next(0, 10).ToString()[0]).ToArray());

        var verificationCode = new VerificationCode
        {
            Email = email,
            Phone = phone,
            Code = code,
            Type = type,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_securityOptions.VerificationCodeExpirationMinutes)
        };

        _context.VerificationCodes.Add(verificationCode);
        await _context.SaveChangesAsync();

        return verificationCode;
    }

    public async Task<bool> ValidateAsync(string? email, string? phone, string code, VerificationCodePurpose purpose)
    {
        var query = _context.VerificationCodes
            .Where(v => v.Code == code && v.Purpose == purpose && v.ExpiresAt > DateTime.UtcNow);

        if (email != null)
        {
            query = query.Where(v => v.Email == email);
        }
        else if (phone != null)
        {
            query = query.Where(v => v.Phone == phone);
        }
        else
        {
            return false;
        }

        var verificationCode = await query.FirstOrDefaultAsync();
        if (verificationCode == null)
        {
            return false;
        }

        verificationCode.RetryCount++;

        if (verificationCode.RetryCount >= _securityOptions.MaxVerificationCodeRetry)
        {
            _context.VerificationCodes.Remove(verificationCode);
        }

        await _context.SaveChangesAsync();
        return verificationCode.RetryCount <= _securityOptions.MaxVerificationCodeRetry;
    }

    public async Task DeleteAsync(Guid id)
    {
        var code = await _context.VerificationCodes.FindAsync(id);
        if (code != null)
        {
            _context.VerificationCodes.Remove(code);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteExpiredAsync()
    {
        var expiredCodes = await _context.VerificationCodes
            .Where(v => v.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        _context.VerificationCodes.RemoveRange(expiredCodes);
        await _context.SaveChangesAsync();
    }
}
