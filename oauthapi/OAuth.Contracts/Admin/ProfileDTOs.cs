using System.ComponentModel.DataAnnotations;

namespace OAuth.Contracts.Admin;

public class ProfileResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminUpdateRequest
{
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
}

public class AvatarUploadResponse
{
    public string Message { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

public class AdminChangePasswordRequest
{
    [Required(ErrorMessage = "请输入当前密码")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入新密码")]
    [MinLength(8, ErrorMessage = "密码长度不能少于8位")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{8,}$", ErrorMessage = "密码需包含大小写字母和数字")]
    public string NewPassword { get; set; } = string.Empty;
}
