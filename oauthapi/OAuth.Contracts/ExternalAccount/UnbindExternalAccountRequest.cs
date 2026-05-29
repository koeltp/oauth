using OAuth.Domain.Entities;

namespace OAuth.Contracts.ExternalAccount;

public class UnbindExternalAccountRequest
{
    public ExternalProvider Provider { get; set; }
}
