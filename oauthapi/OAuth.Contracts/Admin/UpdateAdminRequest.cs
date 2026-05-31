using System.ComponentModel.DataAnnotations;
using OAuth.Domain.Entities;

namespace OAuth.Contracts.Admin;

public class UpdateAdminRequest
{
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string? Email { get; set; }
    public AdminRole? Role { get; set; }
}