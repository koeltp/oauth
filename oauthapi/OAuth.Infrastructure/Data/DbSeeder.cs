using Microsoft.EntityFrameworkCore;
using OAuth.Domain.Entities;

namespace OAuth.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        await SeedAdminsAsync(context);
        await SeedUsersAsync(context);
        await SeedClientsAsync(context);
        await SeedAuthorizationsAsync(context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedAdminsAsync(ApplicationDbContext context)
    {
        if (!await context.Admins.AnyAsync())
        {
            var admins = new List<Admin>
            {
                new()
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Email = "admin@example.com",
                    Role = AdminRole.SuperAdmin,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Username = "operator",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("operator123"),
                    Email = "operator@example.com",
                    Role = AdminRole.Admin,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Admins.AddRange(admins);
        }
    }

    private static async Task SeedUsersAsync(ApplicationDbContext context)
    {
        if (!await context.Users.AnyAsync())
        {
            var users = new List<User>
        {
            new()
            {
                Username = "admin",
                Email = "admin@example.com",
                Phone = "13800138000",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                EmailVerified = true,
                PhoneVerified = true,
                TwoFactorEnabled = false,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Username = "zhangsan",
                Email = "zhangsan@example.com",
                Phone = "13800138001",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                EmailVerified = true,
                PhoneVerified = true,
                TwoFactorEnabled = false,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Username = "lisi",
                Email = "lisi@example.com",
                Phone = "13800138002",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                EmailVerified = true,
                PhoneVerified = false,
                TwoFactorEnabled = false,
                TwoFactorSecret = null,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Username = "wangwu",
                Email = "wangwu@example.com",
                Phone = "13800138003",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                EmailVerified = false,
                PhoneVerified = false,
                TwoFactorEnabled = false,
                Status = UserStatus.Inactive,
                CreatedAt = DateTime.UtcNow
            }
        };

            await context.Users.AddRangeAsync(users);
        }
    }

    private static async Task SeedClientsAsync(ApplicationDbContext context)
    {
        if (!await context.Clients.AnyAsync())
        {
            var clients = new List<Client>
        {
            new()
            {
                ClientId = OpenIddictIdentifier.GenerateClientId(),
                ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(OpenIddictIdentifier.GenerateClientSecret()),
                Name = "测试应用 A",
                Logo = null,
                RedirectUris = "https://app-a.example.com/callback",
                AllowedScopes = "openid profile email",
                Status = ClientStatus.Approved,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ClientId = OpenIddictIdentifier.GenerateClientId(),
                ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(OpenIddictIdentifier.GenerateClientSecret()),
                Name = "测试应用 B",
                Logo = null,
                RedirectUris = "https://app-b.example.com/callback",
                AllowedScopes = "openid profile email phone",
                Status = ClientStatus.Approved,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ClientId = OpenIddictIdentifier.GenerateClientId(),
                ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(OpenIddictIdentifier.GenerateClientSecret()),
                Name = "待审核应用",
                Logo = null,
                RedirectUris = "https://pending.example.com/callback",
                AllowedScopes = "openid profile",
                Status = ClientStatus.Pending,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ClientId = OpenIddictIdentifier.GenerateClientId(),
                ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(OpenIddictIdentifier.GenerateClientSecret()),
                Name = "被拒绝的应用",
                Logo = null,
                RedirectUris = "https://rejected.example.com/callback",
                AllowedScopes = "openid profile",
                Status = ClientStatus.Rejected,
                ReviewerId = null,
                ReviewedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        };

            await context.Clients.AddRangeAsync(clients);
        }
    }

    private static async Task SeedAuthorizationsAsync(ApplicationDbContext context)
    {
        var users = await context.Users.ToListAsync();
        var clients = await context.Clients.Where(c => c.Status == ClientStatus.Approved).ToListAsync();

        if (users.Count > 0 && clients.Count > 0)
        {
            var authorizations = new List<Authorization>
            {
                new()
                {
                    UserId = users[0].Id,
                    ClientId = clients[0].Id,
                    Scope = "openid profile email",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    UserId = users[0].Id,
                    ClientId = clients[1].Id,
                    Scope = "openid profile",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    UserId = users[1].Id,
                    ClientId = clients[0].Id,
                    Scope = "openid profile email phone",
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Authorizations.AddRangeAsync(authorizations);
        }
    }
}
