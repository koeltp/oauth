using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Auth;
using OAuth.Contracts.User;
using OAuth.Domain.Exceptions;
using Taipi.Core.RQRS;

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
    public async Task<ResponseResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request.Username, request.Email, request.Password);
            return new ResponseResult<RegisterResponse>(new RegisterResponse
            {
                UserId = result.UserId,
                Username = result.Username,
                Email = result.Email
            });
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult<RegisterResponse>.BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("login/password")]
    public async Task<ResponseResult<LoginResponse>> LoginWithPassword([FromBody] PasswordLoginRequest request)
    {
        try
        {
            var result = await _authService.LoginWithPasswordAsync(request.Email, request.Password, request.TwoFaCode);
            return new ResponseResult<LoginResponse>(new LoginResponse
            {
                UserId = result.UserId,
                Username = result.Username,
                Email = result.Email,
                TwoFactorEnabled = result.TwoFactorEnabled,
                AccessToken = result.AccessToken,
                TokenType = "Bearer",
                ExpiresIn = result.ExpiresIn,
                IsAdmin = result.IsAdmin
            });
        }
        catch (TwoFactorRequiredException ex)
        {
            return new ResponseResult<LoginResponse>(new LoginResponse { Require2FA = true, UserId = ex.UserId });
        }
        catch (UnauthorizedAccessException ex)
        {
            return ResponseResult<LoginResponse>.Unauthorized(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult<LoginResponse>.BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("login/email-code")]
    public async Task<ResponseResult<SendCodeResponse>> SendEmailCode([FromBody] SendCodeRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return ResponseResult<SendCodeResponse>.BadRequest("请输入邮箱地址");
        }

        var user = await _userService.GetByEmailAsync(request.Email);
        if (user == null && request.Purpose == VerificationCodePurpose.Login)
        {
            return ResponseResult<SendCodeResponse>.BadRequest("用户未找到");
        }

        var expiresIn = await _verificationCodeService.CreateAsync(request.Email, VerificationCodeType.Email, request.Purpose);

        return new ResponseResult<SendCodeResponse>(new SendCodeResponse { ExpiresIn = expiresIn }) { Message = "验证码已发送" };
    }

    [AllowAnonymous]
    [HttpPost("login/sms-code")]
    public async Task<ResponseResult<SendCodeResponse>> SendSmsCode([FromBody] SendCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return ResponseResult<SendCodeResponse>.BadRequest("请输入手机号码");
        }

        var expiresIn = await _verificationCodeService.CreateAsync(request.Phone, VerificationCodeType.Sms, request.Purpose);

        return new ResponseResult<SendCodeResponse>(new SendCodeResponse { ExpiresIn = expiresIn }) { Message = "验证码已发送" };
    }

    [AllowAnonymous]
    [HttpPost("verify-code")]
    public async Task<ResponseResult<LoginResponse>> VerifyCode([FromBody] VerifyCodeRequest request)
    {
        try
        {
            var result = await _authService.VerifyCodeAsync(request.Identifier, request.Type, request.Code, request.Purpose);
            return new ResponseResult<LoginResponse>(new LoginResponse
            {
                UserId = result.UserId,
                Username = result.Username,
                Email = result.Email,
                TwoFactorEnabled = result.TwoFactorEnabled,
                AccessToken = result.AccessToken,
                TokenType = "Bearer",
                ExpiresIn = result.ExpiresIn,
                IsAdmin = result.IsAdmin
            });
        }
        catch (TwoFactorRequiredException ex)
        {
            return new ResponseResult<LoginResponse>(new LoginResponse { Require2FA = true, UserId = ex.UserId });
        }
        catch (UnauthorizedAccessException ex)
        {
            return ResponseResult<LoginResponse>.Unauthorized(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult<LoginResponse>.BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("2fa/verify")]
    public async Task<ResponseResult<LoginResponse>> Verify2FA([FromBody] Verify2FARequest request)
    {
        try
        {
            var result = await _authService.Verify2FAAsync(request.UserId, request.Code);
            return new ResponseResult<LoginResponse>(new LoginResponse
            {
                UserId = result.UserId,
                Username = result.Username,
                Email = result.Email,
                TwoFactorEnabled = result.TwoFactorEnabled,
                AccessToken = result.AccessToken,
                TokenType = "Bearer",
                ExpiresIn = result.ExpiresIn,
                IsAdmin = result.IsAdmin
            });
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult<LoginResponse>.BadRequest(ex.Message);
        }
    }

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<ResponseResult<TwoFactorSetupResponse>> Enable2FA()
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return ResponseResult<TwoFactorSetupResponse>.Unauthorized("未授权");
        }

        try
        {
            var result = await _authService.Enable2FAAsync(userId.Value);
            return new ResponseResult<TwoFactorSetupResponse>(new TwoFactorSetupResponse
            {
                Secret = result.Secret,
                QrCodeUrl = result.QrCodeUrl
            });
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult<TwoFactorSetupResponse>.NotFound(ex.Message);
        }
    }

    [HttpPost("2fa/confirm")]
    [Authorize]
    public async Task<ResponseResult<object>> Confirm2FA([FromBody] Confirm2FARequest request)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return ResponseResult<object>.Unauthorized("未授权");
        }

        try
        {
            await _authService.Confirm2FAAsync(userId.Value, request.Code);
            return new ResponseResult<object> { Message = "两步验证已开启" };
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult<object>.BadRequest(ex.Message);
        }
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<ResponseResult<UserInfoResponse>> Disable2FA([FromBody] Disable2FARequest request)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return ResponseResult<UserInfoResponse>.Unauthorized("未授权");
        }

        try
        {
            await _authService.Disable2FAAsync(userId.Value, request.Code);
            return new ResponseResult<UserInfoResponse> { Message = "两步验证已禁用" };
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult<UserInfoResponse>.BadRequest(ex.Message);
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ResponseResult<UserInfoResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return ResponseResult<UserInfoResponse>.Unauthorized("未授权");
        }

        try
        {
            await _authService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);
            return new ResponseResult<UserInfoResponse> { Message = "密码修改成功" };
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult<UserInfoResponse>.BadRequest(ex.Message);
        }
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ResponseResult<UserInfoResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return ResponseResult<UserInfoResponse>.Unauthorized("未授权");
        }

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null)
        {
            return ResponseResult<UserInfoResponse>.NotFound("用户未找到");
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

        return new ResponseResult<UserInfoResponse>(new UserInfoResponse
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
        });
    }

    [HttpPost("bind-phone")]
    [Authorize]
    public async Task<ResponseResult<UserInfoResponse>> BindPhone([FromBody] BindPhoneRequest request)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return ResponseResult<UserInfoResponse>.Unauthorized("未授权");
        }

        try
        {
            await _authService.BindPhoneAsync(userId.Value, request.Phone, request.Code);
            var user = await _userService.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return ResponseResult<UserInfoResponse>.NotFound("用户未找到");
            }
            return new ResponseResult<UserInfoResponse>(new UserInfoResponse
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
            })
            { Message = "手机绑定成功" };
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult<UserInfoResponse>.BadRequest(ex.Message);
        }
    }
}