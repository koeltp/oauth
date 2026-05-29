using System.ComponentModel.DataAnnotations;

namespace OAuth.Contracts.Admin;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}