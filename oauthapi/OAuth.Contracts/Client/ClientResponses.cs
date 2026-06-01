namespace OAuth.Contracts.Client;

public class ClientResponse
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Description { get; set; }
    public string RedirectUris { get; set; } = string.Empty;
    public string AllowedScopes { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ClientRegisteredResponse
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public bool IsPublic { get; set; }
}

public class ClientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string? Description { get; set; }
    public string? Logo { get; set; }
    public bool IsPublic { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RedirectUris { get; set; }
    public string? AllowedScopes { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}