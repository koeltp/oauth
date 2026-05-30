using System.ComponentModel.DataAnnotations;

namespace OAuth.Contracts.Client;

public class RegisterClientRequest : IValidatableObject
{
    [Required(ErrorMessage = "应用名称不能为空")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "回调地址不能为空")]
    public string RedirectUris { get; set; } = string.Empty;

    public string AllowedScopes { get; set; } = "openid profile email";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(RedirectUris))
        {
            yield return new ValidationResult("回调地址不能为空", new[] { nameof(RedirectUris) });
            yield break;
        }

        var uris = RedirectUris.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim())
            .Where(u => u.Length > 0)
            .ToList();

        if (uris.Count == 0)
        {
            yield return new ValidationResult("回调地址不能为空", new[] { nameof(RedirectUris) });
            yield break;
        }

        foreach (var uri in uris)
        {
            if (uri.StartsWith('/'))
            {
                continue;
            }

            if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) ||
                (parsed.Scheme != "http" && parsed.Scheme != "https"))
            {
                yield return new ValidationResult($"无效的回调地址: {uri}", new[] { nameof(RedirectUris) });
            }
        }
    }
}
