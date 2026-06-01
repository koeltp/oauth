using OAuth.Contracts.Auth;

namespace OAuth.Application.Interfaces;

public interface IVerificationCodeService
{
    Task<int> CreateAsync(string identifier, VerificationCodeType type, VerificationCodePurpose purpose);
    Task<bool> ValidateAsync(string identifier, string code, VerificationCodePurpose purpose);
}
