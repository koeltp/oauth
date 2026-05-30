using System.ComponentModel.DataAnnotations;

namespace OAuth.Contracts.Auth;

public class PasswordLoginRequest
{
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;

    public string? TwoFaCode { get; set; }
}
