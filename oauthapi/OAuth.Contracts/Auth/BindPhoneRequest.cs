using System.ComponentModel.DataAnnotations;

namespace OAuth.Contracts.Auth;

public class BindPhoneRequest
{
    [Required(ErrorMessage = "手机号不能为空")]
    [Phone(ErrorMessage = "请输入有效的手机号")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "验证码不能为空")]
    public string Code { get; set; } = string.Empty;
}
