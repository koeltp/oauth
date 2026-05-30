using System.ComponentModel.DataAnnotations;
using OAuth.Domain.Entities;

namespace OAuth.Contracts.Admin;

public class CreateAdminRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [MinLength(8, ErrorMessage = "密码长度不能少于8位")]
    public string Password { get; set; } = string.Empty;

    public AdminRole Role { get; set; } = AdminRole.Operator;
}
