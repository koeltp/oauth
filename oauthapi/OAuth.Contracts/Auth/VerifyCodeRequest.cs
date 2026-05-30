using System.ComponentModel.DataAnnotations;
using OAuth.Domain.Entities;

namespace OAuth.Contracts.Auth;

public class VerifyCodeRequest
{
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "请输入有效的手机号")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "验证码不能为空")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "验证码用途不能为空")]
    public VerificationCodePurpose Purpose { get; set; }
}
