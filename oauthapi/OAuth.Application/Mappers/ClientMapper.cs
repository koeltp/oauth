using OAuth.Contracts.Client;
using OAuth.Domain.Entities;

namespace OAuth.Application.Mappers;

public static class ClientMapper
{
    public static ClientDto ToDto(this Client client)
    {
        return new ClientDto
        {
            Id = client.Id,
            Name = client.Name,
            ClientId = client.ClientId,
            Description = client.Description,
            Logo = client.Logo,
            IsPublic = client.IsPublic,
            Status = client.Status.ToString(),
            RedirectUris = client.RedirectUris,
            AllowedScopes = client.AllowedScopes,
            ReviewerName = client.Reviewer?.Username,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        };
    }
}