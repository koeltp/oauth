using System.ComponentModel.DataAnnotations;
using OAuth.Domain.Entities;

namespace OAuth.Contracts.Auth;

public class SendCodeRequest
{
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "请输入有效的手机号")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "验证码用途不能为空")]
    public VerificationCodePurpose Purpose { get; set; }
}
