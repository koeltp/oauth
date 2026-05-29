using OAuth.Domain.Entities;

namespace OAuth.Contracts.ExternalAccount;

public class BindExternalAccountRequest
{
    public ExternalProvider Provider { get; set; }
    public string ProviderUserId { get; set; } = string.Empty;
}
