using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Auth;
using OAuth.Domain.Entities;
using OAuth.Domain.Exceptions;
using OAuth.Infrastructure.Helpers;
using OAuth.Infrastructure.Options;
using System.Text.Json;

namespace OAuth.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly IVerificationCodeService _verificationCodeService;
    private readonly IAdminService _adminService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly TokenOptions _tokenOptions;
    private readonly IDistributedCache _cache;
    private readonly IEmailService _emailService;

    public AuthService(
        IUserService userService,
        IVerificationCodeService verificationCodeService,
        IAdminService adminService,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IOptions<TokenOptions> tokenOptions,
        IDistributedCache cache,
        IEmailService emailService)
    {
        _userService = userService;
        _verificationCodeService = verificationCodeService;
        _adminService = adminService;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _tokenOptions = tokenOptions.Value;
        _cache = cache;
        _emailService = emailService;
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
            throw new InvalidOperationException("邮箱或密码错误");
        }

        var isValid = await _userService.ValidatePasswordAsync(user, password);
        if (!isValid)
        {
            throw new InvalidOperationException("邮箱或密码错误");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new InvalidOperationException(user.Status == UserStatus.Banned ? "账户已被禁用" : "账户未激活");
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

    public async Task<AuthLoginResult> RefreshTokenAsync(string refreshToken)
    {
        var token = await _refreshTokenService.GetByTokenAsync(refreshToken);
        if (token == null || token.Revoked || token.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("刷新令牌无效或已过期");
        }

        await _refreshTokenService.RevokeAsync(refreshToken);

        var user = await _userService.GetByIdAsync(token.UserId);
        if (user == null || user.Status != UserStatus.Active)
        {
            throw new InvalidOperationException("用户不存在或已被禁用");
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
            throw new InvalidOperationException("用户不存在");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new InvalidOperationException(user.Status == UserStatus.Banned ? "账户已被禁用" : "账户未激活");
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
        var refreshToken = await _refreshTokenService.CreateAsync(user.Id);

        return new AuthLoginResult(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            TwoFactorEnabled: user.TwoFactorEnabled,
            AccessToken: token,
            ExpiresIn: _jwtService.GetExpirationSeconds(),
            IsAdmin: admin != null,
            RefreshToken: refreshToken.Token,
            RefreshExpiresIn: _tokenOptions.RefreshTokenExpirationSeconds
        );
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _userService.GetByEmailAsync(email);
        if (user == null)
        {
            return;
        }

        await _verificationCodeService.CreateAsync(email, VerificationCodeType.Email, VerificationCodePurpose.ResetPassword);
    }

    public async Task ResetPasswordAsync(string email, string code, string newPassword)
    {
        var user = await _userService.GetByEmailAsync(email);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        var isValid = await _verificationCodeService.ValidateAsync(email, code, VerificationCodePurpose.ResetPassword);
        if (!isValid)
        {
            throw new InvalidOperationException("验证码无效或已过期");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _userService.UpdateAsync(user);
    }

    public async Task SendVerificationEmailAsync(Guid userId, string frontendBaseUrl)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        if (user.EmailVerified)
        {
            throw new InvalidOperationException("邮箱已验证");
        }

        var token = Guid.NewGuid().ToString("N");
        var cacheKey = $"email_verify:{token}";

        await _cache.SetStringAsync(cacheKey, user.Id.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });

        var verificationUrl = $"{frontendBaseUrl}/verify-email?token={token}";
        await _emailService.SendAsync(user.Email, "验证您的邮箱",
            $"请点击以下链接验证您的邮箱：<br/><a href='{verificationUrl}'>{verificationUrl}</a>");
    }

    public async Task VerifyEmailAsync(string token)
    {
        var cacheKey = $"email_verify:{token}";
        var userIdStr = await _cache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(userIdStr))
        {
            throw new InvalidOperationException("验证链接无效或已过期");
        }

        if (!Guid.TryParse(userIdStr, out var userId))
        {
            throw new InvalidOperationException("验证链接无效");
        }

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        user.EmailVerified = true;
        await _userService.UpdateAsync(user);

        await _cache.RemoveAsync(cacheKey);
    }
}

