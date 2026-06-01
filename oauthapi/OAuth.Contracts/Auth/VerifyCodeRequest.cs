using System.ComponentModel.DataAnnotations;

namespace OAuth.Contracts.Auth;

public class VerifyCodeRequest : IValidatableObject
{
    [Required(ErrorMessage = "邮箱/手机号不能为空")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "验证码类型不能为空")]
    public VerificationCodeType Type { get; set; }

    [Required(ErrorMessage = "验证码不能为空")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "验证码用途不能为空")]
    public VerificationCodePurpose Purpose { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == VerificationCodeType.Email)
        {
            if (!new EmailAddressAttribute().IsValid(Identifier))
                yield return new ValidationResult("请输入有效的邮箱地址", new[] { nameof(Identifier) });
        }
        else if (Type == VerificationCodeType.Sms)
        {
            if (!new PhoneAttribute().IsValid(Identifier))
                yield return new ValidationResult("请输入有效的手机号", new[] { nameof(Identifier) });
        }
    }
}
