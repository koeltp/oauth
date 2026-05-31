using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Auth;
using OAuth.Contracts.Common;
using OAuth.Contracts.User;
using OAuth.Domain.Entities;
using OAuth.Domain.Exceptions;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly IVerificationCodeService _verificationCodeService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(
        IAuthService authService,
        IUserService userService,
        IVerificationCodeService verificationCodeService,
        ICurrentUserService currentUserService)
    {
        _authService = authService;
        _userService = userService;
        _verificationCodeService = verificationCodeService;
        _currentUserService = currentUserService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ApiResponse<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request.Username, request.Email, request.Password);
            return new ApiResponse<RegisterResponse>
            {
                Data = new RegisterResponse
                {
                    UserId = result.UserId,
                    Username = result.Username,
                    Email = result.Email
                }
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResponse<RegisterResponse> { Code = 400, Message = ex.Message };
        }
    }

    [AllowAnonymous]
    [HttpPost("login/password")]
    public async Task<ApiResponse<LoginResponse>> LoginWithPassword([FromBody] PasswordLoginRequest request)
    {
        try
        {
            var result = await _authService.LoginWithPasswordAsync(request.Email, request.Password, request.TwoFaCode);
            return new ApiResponse<LoginResponse>
            {
                Data = new LoginResponse
                {
                    UserId = result.UserId,
                    Username = result.Username,
                    Email = result.Email,
                    TwoFactorEnabled = result.TwoFactorEnabled,
                    AccessToken = result.AccessToken,
                    TokenType = "Bearer",
                    ExpiresIn = result.ExpiresIn,
                    IsAdmin = result.IsAdmin
                }
            };
        }
        catch (TwoFactorRequiredException ex)
        {
            return new ApiResponse<LoginResponse> { Data = new LoginResponse { Require2FA = true, UserId = ex.UserId } };
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<LoginResponse> { Code = 401, Message = ex.Message };
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResponse<LoginResponse> { Code = 400, Message = ex.Message };
        }
    }

    [AllowAnonymous]
    [HttpPost("login/email-code")]
    public async Task<ApiResponse<SendCodeResponse>> SendEmailCode([FromBody] SendCodeRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return new ApiResponse<SendCodeResponse> { Code = 400, Message = "请输入邮箱地址" };
        }

        var user = await _userService.GetByEmailAsync(request.Email);
        if (user == null && request.Purpose == VerificationCodePurpose.Login)
        {
            return new ApiResponse<SendCodeResponse> { Code = 400, Message = "用户未找到" };
        }

        await _verificationCodeService.CreateAsync(request.Email, null, request.Purpose, VerificationCodeType.Email);

        return new ApiResponse<SendCodeResponse> { Data = new SendCodeResponse { ExpiresIn = 300 }, Message = "验证码已发送" };
    }

    [AllowAnonymous]
    [HttpPost("login/sms-code")]
    public async Task<ApiResponse<SendCodeResponse>> SendSmsCode([FromBody] SendCodeRequest request)
    {
        if (string.IsNullOrEmpty(request.Phone))
        {
            return new ApiResponse<SendCodeResponse> { Code = 400, Message = "请输入手机号码" };
        }

        await _verificationCodeService.CreateAsync(null, request.Phone, request.Purpose, VerificationCodeType.Sms);

        return new ApiResponse<SendCodeResponse> { Data = new SendCodeResponse { ExpiresIn = 300 }, Message = "验证码已发送" };
    }

    [AllowAnonymous]
    [HttpPost("verify-code")]
    public async Task<ApiResponse<LoginResponse>> VerifyCode([FromBody] VerifyCodeRequest request)
    {
        try
        {
            var result = await _authService.VerifyCodeAsync(request.Email, request.Phone, request.Code, request.Purpose);
            return new ApiResponse<LoginResponse>
            {
                Data = new LoginResponse
                {
                    UserId = result.UserId,
                    Username = result.Username,
                    Email = result.Email,
                    TwoFactorEnabled = result.TwoFactorEnabled,
                    AccessToken = result.AccessToken,
                    TokenType = "Bearer",
                    ExpiresIn = result.ExpiresIn,
                    IsAdmin = result.IsAdmin
                }
            };
        }
        catch (TwoFactorRequiredException ex)
        {
            return new ApiResponse<LoginResponse> { Data = new LoginResponse { Require2FA = true, UserId = ex.UserId } };
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<LoginResponse> { Code = 401, Message = ex.Message };
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResponse<LoginResponse> { Code = 400, Message = ex.Message };
        }
    }

    [AllowAnonymous]
    [HttpPost("2fa/verify")]
    public async Task<ApiResponse<LoginResponse>> Verify2FA([FromBody] Verify2FARequest request)
    {
        try
        {
            var result = await _authService.Verify2FAAsync(request.UserId, request.Code);
            return new ApiResponse<LoginResponse>
            {
                Data = new LoginResponse
                {
                    UserId = result.UserId,
                    Username = result.Username,
                    Email = result.Email,
                    TwoFactorEnabled = result.TwoFactorEnabled,
                    AccessToken = result.AccessToken,
                    TokenType = "Bearer",
                    ExpiresIn = result.ExpiresIn,
                    IsAdmin = result.IsAdmin
                }
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResponse<LoginResponse> { Code = 400, Message = ex.Message };
        }
    }

    /// <summary>
    /// 开启两步验证
    /// </summary>
    /// <returns></returns>
    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<ApiResponse<TwoFactorSetupResponse>> Enable2FA()
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return new ApiResponse<TwoFactorSetupResponse> { Code = 401, Message = "未授权" };
        }

        try
        {
            var result = await _authService.Enable2FAAsync(userId.Value);
            return new ApiResponse<TwoFactorSetupResponse>
            {
                Data = new TwoFactorSetupResponse
                {
                    Secret = result.Secret,
                    QrCodeUrl = result.QrCodeUrl
                }
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResponse<TwoFactorSetupResponse> { Code = 404, Message = ex.Message };
        }
    }

    [HttpPost("2fa/confirm")]
    [Authorize]
    public async Task<ApiResponse<object>> Confirm2FA([FromBody] Confirm2FARequest request)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return new ApiResponse<object> { Code = 401, Message = "未授权" };
        }

        try
        {
            await _authService.Confirm2FAAsync(userId.Value, request.Code);
            return new ApiResponse<object> { Message = "两步验证已开启" };
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResponse<object> { Code = 400, Message = ex.Message };
        }
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<ApiResponse<UserInfoResponse>> Disable2FA([FromBody] Disable2FARequest request)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return new ApiResponse<UserInfoResponse> { Code = 401, Message = "未授权" };
        }

        try
        {
            await _authService.Disable2FAAsync(userId.Value, request.Code);
            return new ApiResponse<UserInfoResponse> { Message = "两步验证已禁用" };
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResponse<UserInfoResponse> { Code = 400, Message = ex.Message };
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ApiResponse<UserInfoResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return new ApiResponse<UserInfoResponse> { Code = 401, Message = "未授权" };
        }

        try
        {
            await _authService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);
            return new ApiResponse<UserInfoResponse> { Message = "密码修改成功" };
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResponse<UserInfoResponse> { Code = 400, Message = ex.Message };
        }
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ApiResponse<UserInfoResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return new ApiResponse<UserInfoResponse> { Code = 401, Message = "未授权" };
        }

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null)
        {
            return new ApiResponse<UserInfoResponse> { Code = 404, Message = "用户未找到" };
        }

        if (!string.IsNullOrEmpty(request.Username))
        {
            user.Username = request.Username;
        }

        if (!string.IsNullOrEmpty(request.Phone))
        {
            user.Phone = request.Phone;
        }

        await _userService.UpdateAsync(user);

        return new ApiResponse<UserInfoResponse>
        {
            Data = new UserInfoResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                EmailVerified = user.EmailVerified,
                PhoneVerified = user.PhoneVerified,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt
            }
        };
    }

    [HttpPost("bind-phone")]
    [Authorize]
    public async Task<ApiResponse<UserInfoResponse>> BindPhone([FromBody] BindPhoneRequest request)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return new ApiResponse<UserInfoResponse> { Code = 401, Message = "未授权" };
        }

        try
        {
            await _authService.BindPhoneAsync(userId.Value, request.Phone, request.Code);
            var user = await _userService.GetByIdAsync(userId.Value);
            return new ApiResponse<UserInfoResponse>
            {
                Data = new UserInfoResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Phone = user.Phone,
                    EmailVerified = user.EmailVerified,
                    PhoneVerified = user.PhoneVerified,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    Status = user.Status.ToString(),
                    CreatedAt = user.CreatedAt
                },
                Message = "手机绑定成功"
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResponse<UserInfoResponse> { Code = 400, Message = ex.Message };
        }
    }
}