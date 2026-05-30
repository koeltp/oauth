using System.ComponentModel.DataAnnotations;

namespace OAuth.Contracts.Auth;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "请输入当前密码")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入新密码")]
    [MinLength(8, ErrorMessage = "密码长度不能少于8位")]
    public string NewPassword { get; set; } = string.Empty;
}
