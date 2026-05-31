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
            Status = client.Status.ToString(),
            RedirectUris = client.RedirectUris,
            AllowedScopes = client.AllowedScopes,
            CreatedAt = client.CreatedAt
        };
    }
}