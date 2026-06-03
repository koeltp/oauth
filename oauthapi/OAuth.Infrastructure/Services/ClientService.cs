using Microsoft.EntityFrameworkCore;
using OAuth.Application.Interfaces;
using OAuth.Application.Mappers;
using OAuth.Contracts.Client;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Data;
using OpenIddict.Abstractions;
using Taipi.Core.Linq;
using Taipi.Core.RQRS;

namespace OAuth.Infrastructure.Services;

public class ClientService : IClientService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IOpenIddictApplicationManager _applicationManager;

    public ClientService(ApplicationDbContext context, IEncryptionService encryptionService, IOpenIddictApplicationManager applicationManager)
    {
        _context = context;
        _encryptionService = encryptionService;
        _applicationManager = applicationManager;
    }

    public async Task<PagerResponse<ClientDto>> GetListAsync(SearchPager<ClientSearchDto> query)
    {
        var q = _context.Clients
            .Include(c => c.Reviewer)
            .AsQueryable();

        q = q.WhereIf(!string.IsNullOrEmpty(query.Condition?.Name), c => c.Name.Contains(query.Condition!.Name!)) ;

        q = q.WhereIf(!string.IsNullOrEmpty(query.Condition?.Status), c =>
            c.Status.ToString() == query.Condition!.Status);

        var total = await q.CountAsync();

        var items = await q
            .OrderBy(c => c.Name)
            .Page(query)
            .Select(c => c.ToDto())
            .ToListAsync();

        return new PagerResponse<ClientDto>
        {
            Items = items,
            TotalCount = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    public async Task<List<Client>> GetPendingAsync()
    {
        return await _context.Clients
            .Where(c => c.Status == ClientStatus.Pending)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Client>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Clients
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Client?> GetByIdAsync(Guid id)
    {
        return await _context.Clients.FindAsync(id);
    }

    public async Task<Client?> GetByClientIdAsync(string clientId)
    {
        return await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
    }

    public async Task<(Client Client, string ClientSecret)> CreateAsync(string name, string? description, string? logo, string redirectUris, string allowedScopes, Guid? userId = null, bool isPublic = false)
    {
        if (userId.HasValue)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId.Value);
            if (!userExists)
            {
                throw new InvalidOperationException("关联的用户不存在");
            }
        }

        var clientSecret = isPublic ? string.Empty : OpenIddictIdentifier.GenerateClientSecret();
        var client = new Client
        {
            ClientId = OpenIddictIdentifier.GenerateClientId(),
            ClientSecretHash = isPublic ? string.Empty : BCrypt.Net.BCrypt.HashPassword(clientSecret),
            ClientSecretEncrypted = isPublic ? string.Empty : _encryptionService.Encrypt(clientSecret),
            Name = name,
            Description = description,
            Logo = logo,
            RedirectUris = redirectUris,
            AllowedScopes = allowedScopes,
            IsPublic = isPublic,
            UserId = userId
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        return (client, clientSecret);
    }

    public async Task SubmitAsync(Guid id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client != null && client.Status == ClientStatus.Draft)
        {
            client.Status = ClientStatus.Pending;
            client.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ApproveAsync(Guid id, Guid reviewerId)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client != null)
        {
            client.Status = ClientStatus.Approved;
            client.ReviewerId = reviewerId;
            client.ReviewedAt = DateTime.UtcNow;

            await SyncToOpenIddictAsync(client);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RejectAsync(Guid id, Guid reviewerId)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client != null)
        {
            client.Status = ClientStatus.Rejected;
            client.ReviewerId = reviewerId;
            client.ReviewedAt = DateTime.UtcNow;

            await RemoveFromOpenIddictAsync(client.ClientId);
            await _context.SaveChangesAsync();
        }
    }

    public async Task WithdrawAsync(Guid id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client != null && client.Status != ClientStatus.Draft)
        {
            client.Status = ClientStatus.Draft;
            client.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(Client client)
    {
        client.UpdatedAt = DateTime.UtcNow;
        _context.Clients.Update(client);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client != null)
        {
            await RemoveFromOpenIddictAsync(client.ClientId);
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalClientsCount()
    {
        return await _context.Clients.CountAsync();
    }

    public async Task<int> GetClientsCountByStatus(ClientStatus status)
    {
        return await _context.Clients.CountAsync(c => c.Status == status);
    }

    private async Task SyncToOpenIddictAsync(Client client)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            DisplayName = client.Name,
            ClientType = client.IsPublic ? "public" : "confidential",
            ConsentType = "explicit"
        };

        if (!client.IsPublic && !string.IsNullOrEmpty(client.ClientSecretEncrypted))
        {
            descriptor.ClientSecret = _encryptionService.Decrypt(client.ClientSecretEncrypted);
        }

        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);

        if (!client.IsPublic)
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
        }

        if (!string.IsNullOrEmpty(client.RedirectUris))
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);

            foreach (var uri in client.RedirectUris.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = uri.Trim();
                if (Uri.TryCreate(trimmed, UriKind.Absolute, out var parsed))
                {
                    descriptor.RedirectUris.Add(parsed);
                }
            }
        }

        foreach (var scope in (client.AllowedScopes ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        var existing = await _applicationManager.FindByClientIdAsync(client.ClientId);
        if (existing != null)
        {
            await _applicationManager.UpdateAsync(existing, descriptor);
        }
        else
        {
            await _applicationManager.CreateAsync(descriptor);
        }
    }

    private async Task RemoveFromOpenIddictAsync(string clientId)
    {
        var existing = await _applicationManager.FindByClientIdAsync(clientId);
        if (existing != null)
        {
            await _applicationManager.DeleteAsync(existing);
        }
    }
}