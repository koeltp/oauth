using System.ComponentModel.DataAnnotations;

namespace OAuth.Contracts.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    [MinLength(2, ErrorMessage = "用户名长度不能少于2位")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [MinLength(8, ErrorMessage = "密码长度不能少于8位")]
    public string Password { get; set; } = string.Empty;
}
