using System.ComponentModel.DataAnnotations;

namespace OAuth.Contracts.Client;

public class UpdateClientRequest
{
    [Required(ErrorMessage = "应用名称不能为空")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Logo { get; set; }

    [Required(ErrorMessage = "回调地址不能为空")]
    public string RedirectUris { get; set; } = string.Empty;

    public string AllowedScopes { get; set; } = "openid profile email";
}