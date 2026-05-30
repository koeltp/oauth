using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OAuth.Contracts.Auth;

public class Verify2FARequest
{
    [Required(ErrorMessage = "用户ID不能为空")]
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "验证码不能为空")]
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}
