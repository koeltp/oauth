using OAuth.Domain.Entities;

namespace OAuth.Application.Interfaces;

public interface IVerificationCodeService
{
    Task<VerificationCode> CreateAsync(string? email, string? phone, VerificationCodePurpose purpose, VerificationCodeType type = VerificationCodeType.Email);
    Task<bool> ValidateAsync(string? email, string? phone, string code, VerificationCodePurpose purpose);
    Task DeleteAsync(Guid id);
    Task DeleteExpiredAsync();
}
