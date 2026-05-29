using Microsoft.EntityFrameworkCore;
using OAuth.Domain.Entities;

namespace OAuth.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Authorization> Authorizations => Set<Authorization>();
    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();
    public DbSet<UserExternalAccount> UserExternalAccounts => Set<UserExternalAccount>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AdminRefreshToken> AdminRefreshTokens => Set<AdminRefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // User
        builder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Phone);
        });

        // Client
        builder.Entity<Client>(entity =>
        {
            entity.HasIndex(e => e.ClientId).IsUnique();
        });

        // Admin
        builder.Entity<Admin>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // VerificationCode
        builder.Entity<VerificationCode>(entity =>
        {
            entity.HasIndex(e => new { e.Email, e.Code, e.Purpose });
            entity.HasIndex(e => new { e.Phone, e.Code, e.Purpose });
            entity.HasIndex(e => e.ExpiresAt);
        });

        // UserExternalAccount
        builder.Entity<UserExternalAccount>(entity =>
        {
            entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
        });

        // RefreshToken
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ClientId);
            entity.HasIndex(e => e.ExpiresAt);
        });

        // AdminRefreshToken
        builder.Entity<AdminRefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.AdminId);
            entity.HasIndex(e => e.ExpiresAt);
        });

        // OpenIddict
        builder.UseOpenIddict();
    }
}
