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
        await context.SaveChangesAsync();
        await SeedUsersAsync(context);
        await context.SaveChangesAsync();
        await SeedClientsAsync(context);
        await context.SaveChangesAsync();
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
                Email = "koeltp@163.com",
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
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            var zhangsan = await context.Users.FirstOrDefaultAsync(u => u.Username == "zhangsan");

            var clients = new List<Client>
            {
                new()
                {
                    ClientId = "test-client-id",
                    ClientSecretHash = BCrypt.Net.BCrypt.HashPassword("test-client-secret"),
                    ClientSecretEncrypted = string.Empty,
                    Name = "测试应用 A",
                    Description = "用于 TestClient.Web 的标准登录测试",
                    Logo = "https://sso.taipi.top/assets/logo-DB0P-MWr.png",
                    RedirectUris = "http://localhost:5002/callback",
                    AllowedScopes = "openid profile email",
                    Status = ClientStatus.Approved,
                    UserId = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    ClientId = "test-client-b",
                    ClientSecretHash = BCrypt.Net.BCrypt.HashPassword("test-client-secret-b"),
                    ClientSecretEncrypted = string.Empty,
                    Name = "测试应用 B",
                    Description = "用于跨客户端测试的第二个应用",
                    Logo = "https://sso.taipi.top/assets/logo-DB0P-MWr.png",
                    RedirectUris = "http://localhost:5002/callback",
                    AllowedScopes = "openid profile email phone",
                    Status = ClientStatus.Approved,
                    UserId = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    ClientId = "test-public-client",
                    ClientSecretHash = string.Empty,
                    ClientSecretEncrypted = string.Empty,
                    Name = "公开测试客户端",
                    Description = "用于 TestClient.Spa 的纯前端 PKCE 登录测试",
                    Logo = "https://sso.taipi.top/assets/logo-DB0P-MWr.png",
                    RedirectUris = "http://localhost:5003/callback",
                    AllowedScopes = "openid profile email",
                    IsPublic = true,
                    Status = ClientStatus.Approved,
                    UserId = adminUser?.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    ClientId = OpenIddictIdentifier.GenerateClientId(),
                    ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(OpenIddictIdentifier.GenerateClientSecret()),
                    ClientSecretEncrypted = string.Empty,
                    Name = "待审核应用",
                    Description = "等待管理员审核的测试应用",
                    Logo = "https://sso.taipi.top/assets/logo-DB0P-MWr.png",
                    RedirectUris = "https://pending.taipi.top/callback",
                    AllowedScopes = "openid profile",
                    Status = ClientStatus.Pending,
                    UserId = zhangsan?.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    ClientId = OpenIddictIdentifier.GenerateClientId(),
                    ClientSecretHash = BCrypt.Net.BCrypt.HashPassword(OpenIddictIdentifier.GenerateClientSecret()),
                    ClientSecretEncrypted = string.Empty,
                    Name = "被拒绝的应用",
                    Description = "因不符合规范被拒绝的示例应用",
                    Logo = "https://sso.taipi.top/assets/logo-DB0P-MWr.png",
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
        var clients = await context.Clients.Where(c => c.Status == ClientStatus.Approved).ToListAsync();
        if (clients.Count < 2) return;

        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        var zhangsan = await context.Users.FirstOrDefaultAsync(u => u.Username == "zhangsan");

        var authorizations = new List<Authorization>();
        if (adminUser != null)
        {
            authorizations.AddRange([
                new()
                {
                    UserId = adminUser.Id,
                    ClientId = clients[0].Id,
                    Scope = "openid profile email",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    UserId = adminUser.Id,
                    ClientId = clients[1].Id,
                    Scope = "openid profile",
                    CreatedAt = DateTime.UtcNow
                }
            ]);
        }
        if (zhangsan != null)
        {
            authorizations.Add(new()
            {
                UserId = zhangsan.Id,
                ClientId = clients[0].Id,
                Scope = "openid profile email phone",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (authorizations.Count > 0)
        {
            await context.Authorizations.AddRangeAsync(authorizations);
        }
    }
}
