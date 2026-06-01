using OAuth.Application.Interfaces;
using OAuth.Contracts.Auth;
using OAuth.Domain.Entities;
using OAuth.Domain.Exceptions;
using OAuth.Infrastructure.Helpers;

namespace OAuth.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly IVerificationCodeService _verificationCodeService;
    private readonly IAdminService _adminService;
    private readonly IJwtService _jwtService;

    public AuthService(
        IUserService userService,
        IVerificationCodeService verificationCodeService,
        IAdminService adminService,
        IJwtService jwtService)
    {
        _userService = userService;
        _verificationCodeService = verificationCodeService;
        _adminService = adminService;
        _jwtService = jwtService;
    }

    public async Task<AuthRegisterResult> RegisterAsync(string username, string email, string password)
    {
        var existingUser = await _userService.GetByEmailAsync(email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("邮箱已注册");
        }

        var user = new User
        {
            Username = username,
            Email = email
        };

        await _userService.CreateAsync(user, password);
        return new AuthRegisterResult(user.Id, user.Username, user.Email);
    }

    public async Task<AuthLoginResult> LoginWithPasswordAsync(string email, string password, string? twoFaCode)
    {
        var user = await _userService.GetByEmailAsync(email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("邮箱或密码错误");
        }

        var isValid = await _userService.ValidatePasswordAsync(user, password);
        if (!isValid)
        {
            throw new UnauthorizedAccessException("邮箱或密码错误");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException(user.Status == UserStatus.Banned ? "账户已被禁用" : "账户未激活");
        }

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrEmpty(twoFaCode))
            {
                throw new TwoFactorRequiredException(user.Id);
            }

            if (!TotpHelper.Validate(user.TwoFactorSecret, twoFaCode))
            {
                throw new InvalidOperationException("两步验证码无效");
            }
        }

        return await GenerateLoginResult(user);
    }

    public async Task<AuthLoginResult> VerifyCodeAsync(string identifier, VerificationCodeType type, string code, VerificationCodePurpose purpose)
    {
        var isValid = await _verificationCodeService.ValidateAsync(identifier, code, purpose);
        if (!isValid)
        {
            throw new InvalidOperationException("验证码无效或已过期");
        }

        if (type != VerificationCodeType.Email)
        {
            throw new InvalidOperationException("仅支持邮箱验证码登录");
        }

        var user = await _userService.GetByEmailAsync(identifier);
        if (user == null)
        {
            throw new UnauthorizedAccessException("用户不存在");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException(user.Status == UserStatus.Banned ? "账户已被禁用" : "账户未激活");
        }

        if (user.TwoFactorEnabled)
        {
            throw new TwoFactorRequiredException(user.Id);
        }

        return await GenerateLoginResult(user);
    }

    public async Task<AuthLoginResult> Verify2FAAsync(string userId, string code)
    {
        if (!Guid.TryParse(userId, out var id))
        {
            throw new InvalidOperationException("无效的用户ID");
        }

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new InvalidOperationException(user.Status == UserStatus.Banned ? "账户已被禁用" : "账户未激活");
        }

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            throw new InvalidOperationException("两步验证未启用");
        }

        if (!TotpHelper.Validate(user.TwoFactorSecret, code))
        {
            throw new InvalidOperationException("两步验证码无效");
        }

        return await GenerateLoginResult(user);
    }

    public async Task<TwoFactorSetupResult> Enable2FAAsync(Guid userId)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        if (user.TwoFactorEnabled)
        {
            throw new InvalidOperationException("两步验证已开启");
        }

        var secret = TotpHelper.GenerateSecret();
        user.TwoFactorSecret = secret;
        await _userService.UpdateAsync(user);

        var qrCodeUrl = $"otpauth://totp/OAuth:{user.Email}?secret={secret}&issuer=OAuth";
        return new TwoFactorSetupResult(secret, qrCodeUrl);
    }

    public async Task Confirm2FAAsync(Guid userId, string code)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            throw new InvalidOperationException("请先获取两步验证密钥");
        }

        if (user.TwoFactorEnabled)
        {
            throw new InvalidOperationException("两步验证已开启");
        }

        if (!TotpHelper.Validate(user.TwoFactorSecret, code))
        {
            throw new InvalidOperationException("验证码无效");
        }

        user.TwoFactorEnabled = true;
        await _userService.UpdateAsync(user);
    }

    public async Task Disable2FAAsync(Guid userId, string code)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        if (!TotpHelper.Validate(user.TwoFactorSecret, code))
        {
            throw new InvalidOperationException("两步验证码无效");
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        await _userService.UpdateAsync(user);
    }

    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        var isValid = await _userService.ValidatePasswordAsync(user, currentPassword);
        if (!isValid)
        {
            throw new InvalidOperationException("当前密码不正确");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _userService.UpdateAsync(user);
    }

    public async Task BindPhoneAsync(Guid userId, string phone, string code)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        var isValid = await _verificationCodeService.ValidateAsync(phone, code, VerificationCodePurpose.BindPhone);
        if (!isValid)
        {
            throw new InvalidOperationException("验证码无效或已过期");
        }

        user.Phone = phone;
        user.PhoneVerified = true;
        await _userService.UpdateAsync(user);
    }

    private async Task<AuthLoginResult> GenerateLoginResult(User user)
    {
        var admin = await _adminService.GetByUsernameAsync(user.Username);
        var token = _jwtService.GenerateUserToken(user, admin?.Role.ToString());

        return new AuthLoginResult(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            TwoFactorEnabled: user.TwoFactorEnabled,
            AccessToken: token,
            ExpiresIn: _jwtService.GetExpirationSeconds(),
            IsAdmin: admin != null
        );
    }
}

