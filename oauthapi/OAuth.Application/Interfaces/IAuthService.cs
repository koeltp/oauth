using OAuth.Contracts.Auth;

namespace OAuth.Application.Interfaces;

public interface IAuthService
{
    Task<AuthRegisterResult> RegisterAsync(string username, string email, string password);
    Task<AuthLoginResult> LoginWithPasswordAsync(string email, string password, string? twoFaCode);
    Task<AuthLoginResult> VerifyCodeAsync(string identifier, VerificationCodeType type, string code, VerificationCodePurpose purpose);
    Task<AuthLoginResult> Verify2FAAsync(string userId, string code);
    Task<TwoFactorSetupResult> Enable2FAAsync(Guid userId);
    Task Confirm2FAAsync(Guid userId, string code);
    Task Disable2FAAsync(Guid userId, string code);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task BindPhoneAsync(Guid userId, string phone, string code);
}

public record AuthRegisterResult(Guid UserId, string Username, string Email);

public record AuthLoginResult(
    Guid UserId,
    string Username,
    string Email,
    bool TwoFactorEnabled,
    string AccessToken,
    int ExpiresIn,
    bool IsAdmin
);

public record TwoFactorSetupResult(string Secret, string QrCodeUrl);