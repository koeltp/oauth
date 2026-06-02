using System.ComponentModel.DataAnnotations;

namespace OAuth.Contracts.Auth;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "验证码不能为空")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "新密码不能为空")]
    [MinLength(8, ErrorMessage = "密码长度不能少于8位")]
    public string NewPassword { get; set; } = string.Empty;
}