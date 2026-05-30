using Microsoft.EntityFrameworkCore;
using OAuth.Application.Interfaces;
using OAuth.Domain.Entities;
using OAuth.Infrastructure.Data;

namespace OAuth.Infrastructure.Services;

public class ClientService : IClientService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;

    public ClientService(ApplicationDbContext context, IEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    public async Task<List<Client>> GetAllAsync()
    {
        return await _context.Clients
            .Include(c => c.Reviewer)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
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

    public async Task<(Client Client, string ClientSecret)> CreateAsync(string name, string? description, string redirectUris, string allowedScopes, Guid? userId = null)
    {
        if (userId.HasValue)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId.Value);
            if (!userExists)
            {
                throw new InvalidOperationException("关联的用户不存在");
            }
        }

        var clientSecret = OpenIddictIdentifier.GenerateClientSecret();
        var client = new Client
        {
            ClientId = OpenIddictIdentifier.GenerateClientId(),
            ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(clientSecret),
            ClientSecretEncrypted = _encryptionService.Encrypt(clientSecret),
            Name = name,
            Description = description,
            RedirectUris = redirectUris,
            AllowedScopes = allowedScopes,
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
}
