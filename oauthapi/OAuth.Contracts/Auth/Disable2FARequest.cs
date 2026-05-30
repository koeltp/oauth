using System.ComponentModel.DataAnnotations;

namespace OAuth.Contracts.Auth;

public class Disable2FARequest
{
    [Required(ErrorMessage = "验证码不能为空")]
    public string Code { get; set; } = string.Empty;
}
