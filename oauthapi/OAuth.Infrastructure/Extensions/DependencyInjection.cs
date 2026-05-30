using OAuth.Application.Interfaces;
using OAuth.Infrastructure.Data;
using OAuth.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace OAuth.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IVerificationCodeService, VerificationCodeService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IExternalAccountService, ExternalAccountService>();
        services.AddScoped<IOAuthAuthorizationService, AuthorizationService>();
        services.AddScoped<IUserExternalAccountService, UserExternalAccountService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAdminRefreshTokenService, AdminRefreshTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IEncryptionService, EncryptionService>();

        return services;
    }
}
