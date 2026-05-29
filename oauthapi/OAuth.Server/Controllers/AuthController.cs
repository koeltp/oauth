using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Auth;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Helpers;
using System.Security.Claims;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IVerificationCodeService _verificationCodeService;
    private readonly IAdminService _adminService;
    private readonly IJwtService _jwtService;

    public AuthController(
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

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userService.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Email already registered" });
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email
        };

        await _userService.CreateAsync(user, request.Password);
        return Ok(new { message = "User registered successfully" });
    }

    [AllowAnonymous]
    [HttpPost("login/password")]
    public async Task<IActionResult> LoginWithPassword([FromBody] PasswordLoginRequest request)
    {
        var user = await _userService.GetByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var isValid = await _userService.ValidatePasswordAsync(user, request.Password);
        if (!isValid)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        if (user.Status != UserStatus.Active)
        {
            return Unauthorized(new { message = "Account is not active" });
        }

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrEmpty(request.TwoFaCode))
            {
                return Ok(new { require_2fa = true, user_id = user.Id });
            }
            
            if (!TotpHelper.Validate(user.TwoFactorSecret, request.TwoFaCode))
            {
                return BadRequest(new { message = "Invalid 2FA code" });
            }
        }

        return await GenerateLoginResponse(user);
    }

    [AllowAnonymous]
    [HttpPost("login/email-code")]
    public async Task<IActionResult> SendEmailCode([FromBody] SendCodeRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        var user = await _userService.GetByEmailAsync(request.Email);
        if (user == null && request.Purpose == VerificationCodePurpose.Login)
        {
            return BadRequest(new { message = "User not found" });
        }

        await _verificationCodeService.CreateAsync(request.Email, null, request.Purpose, VerificationCodeType.Email);

        return Ok(new { message = "Verification code sent", expires_in = 300 });
    }

    [AllowAnonymous]
    [HttpPost("login/sms-code")]
    public async Task<IActionResult> SendSmsCode([FromBody] SendCodeRequest request)
    {
        if (string.IsNullOrEmpty(request.Phone))
        {
            return BadRequest(new { message = "Phone number is required" });
        }

        await _verificationCodeService.CreateAsync(null, request.Phone, request.Purpose, VerificationCodeType.Sms);

        return Ok(new { message = "Verification code sent", expires_in = 300 });
    }

    [AllowAnonymous]
    [HttpPost("verify-code")]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeRequest request)
    {
        var isValid = await _verificationCodeService.ValidateAsync(
            request.Email, request.Phone, request.Code, request.Purpose);

        if (!isValid)
        {
            return BadRequest(new { message = "Invalid or expired verification code" });
        }

        if (request.Email == null)
        {
            return Ok(new { verified = true });
        }

        var user = await _userService.GetByEmailAsync(request.Email);
        if (user == null)
        {
            return Ok(new { verified = true });
        }

        if (user.TwoFactorEnabled)
        {
            return Ok(new { verified = true, require_2fa = true, user_id = user.Id });
        }

        return await GenerateLoginResponse(user);
    }

    [AllowAnonymous]
    [HttpPost("2fa/verify")]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest request)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            return BadRequest(new { message = "Invalid user ID" });
        }

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            return BadRequest(new { message = "User not found" });
        }

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            return BadRequest(new { message = "2FA is not enabled" });
        }

        if (!TotpHelper.Validate(user.TwoFactorSecret, request.Code))
        {
            return BadRequest(new { message = "Invalid 2FA code" });
        }

        return await GenerateLoginResponse(user);
    }

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> Enable2FA()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var secret = TotpHelper.GenerateSecret();
        user.TwoFactorSecret = secret;
        user.TwoFactorEnabled = true;
        await _userService.UpdateAsync(user);

        return Ok(new { 
            secret, 
            qr_code_url = $"otpauth://totp/OAuth:{user.Email}?secret={secret}&issuer=OAuth" 
        });
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> Disable2FA([FromBody] Disable2FARequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (!TotpHelper.Validate(user.TwoFactorSecret, request.Code))
        {
            return BadRequest(new { message = "Invalid 2FA code" });
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        await _userService.UpdateAsync(user);

        return Ok(new { message = "2FA disabled" });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var isValid = await _userService.ValidatePasswordAsync(user, request.CurrentPassword);
        if (!isValid)
        {
            return BadRequest(new { message = "Current password is incorrect" });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userService.UpdateAsync(user);

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
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

        return Ok(new { message = "Profile updated successfully" });
    }

    [HttpPost("bind-phone")]
    [Authorize]
    public async Task<IActionResult> BindPhone([FromBody] BindPhoneRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var isValid = await _verificationCodeService.ValidateAsync(
            null, request.Phone, request.Code, VerificationCodePurpose.BindPhone);

        if (!isValid)
        {
            return BadRequest(new { message = "Invalid or expired verification code" });
        }

        user.Phone = request.Phone;
        user.PhoneVerified = true;
        await _userService.UpdateAsync(user);

        return Ok(new { message = "Phone bound successfully" });
    }

    /// <summary>
    /// 从 Claims 获取当前用户 ID
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        
        return userId;
    }

    /// <summary>
    /// 生成登录响应（统一响应格式）
    /// </summary>
    private async Task<IActionResult> GenerateLoginResponse(User user)
    {
        var admin = await _adminService.GetByUsernameAsync(user.Username);
        var token = _jwtService.GenerateUserToken(user, admin?.Role.ToString());
        
        return Ok(new 
        { 
            user_id = user.Id, 
            username = user.Username, 
            email = user.Email, 
            two_factor_enabled = user.TwoFactorEnabled, 
            access_token = token, 
            token_type = "Bearer", 
            expires_in = _jwtService.GetExpirationSeconds(), 
            is_admin = admin != null 
        });
    }
}
