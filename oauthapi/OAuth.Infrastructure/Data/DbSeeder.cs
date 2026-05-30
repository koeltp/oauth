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
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Email = "admin@taipi.top",
                    Role = AdminRole.SuperAdmin,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Username = "operator",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Email = "operator@taipi.top",
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
                Email = "admin@taipi.top",
                Phone = "13800138000",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                EmailVerified = true,
                PhoneVerified = true,
                TwoFactorEnabled = false,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Username = "zhangsan",
                Email = "zhangsan@taipi.top",
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
                Email = "lisi@taipi.top",
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
                Email = "wangwu@taipi.top",
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
            var users = await context.Users.Take(2).ToListAsync();

            var clientSecretA = OpenIddictIdentifier.GenerateClientSecret();
            var clientSecretB = OpenIddictIdentifier.GenerateClientSecret();
            var clientSecretC = OpenIddictIdentifier.GenerateClientSecret();
            var clientSecretD = OpenIddictIdentifier.GenerateClientSecret();

            var clients = new List<Client>
            {
                new()
                {
                    ClientId = OpenIddictIdentifier.GenerateClientId(),
                    ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(clientSecretA),
                    ClientSecretEncrypted = "seed-only",
                    Name = "测试应用 A",
                    Description = "这是一个用于测试的 OAuth 应用",
                    Logo = null,
                    RedirectUris = "https://app-a.taipi.top/callback",
                    AllowedScopes = "openid profile email",
                    Status = ClientStatus.Approved,
                    UserId = users.Count > 0 ? users[0].Id : null,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    ClientId = OpenIddictIdentifier.GenerateClientId(),
                    ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(clientSecretB),
                    ClientSecretEncrypted = "seed-only",
                    Name = "测试应用 B",
                    Description = "另一个测试应用，用于多场景验证",
                    Logo = null,
                    RedirectUris = "https://app-b.taipi.top/callback",
                    AllowedScopes = "openid profile email phone",
                    Status = ClientStatus.Approved,
                    UserId = users.Count > 0 ? users[0].Id : null,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    ClientId = OpenIddictIdentifier.GenerateClientId(),
                    ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(clientSecretC),
                    ClientSecretEncrypted = "seed-only",
                    Name = "待审核应用",
                    Description = "等待管理员审核的测试应用",
                    Logo = null,
                    RedirectUris = "https://pending.taipi.top/callback",
                    AllowedScopes = "openid profile",
                    Status = ClientStatus.Pending,
                    UserId = users.Count > 1 ? users[1].Id : null,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    ClientId = OpenIddictIdentifier.GenerateClientId(),
                    ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(clientSecretD),
                    ClientSecretEncrypted = "seed-only",
                    Name = "被拒绝的应用",
                    Description = "因不符合规范被拒绝的示例应用",
                    Logo = null,
                    RedirectUris = "https://rejected.taipi.top/callback",
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
