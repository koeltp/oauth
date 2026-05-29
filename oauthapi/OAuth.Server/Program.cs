using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OAuth.Application.Interfaces;
using OAuth.Infrastructure.Data;
using OAuth.Infrastructure.Extensions;
using OAuth.Infrastructure.Options;
using OAuth.Server.Middlewares;
using OpenIddict.Server;
using System.Security.Claims;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
    ?? throw new InvalidOperationException("Redis:ConnectionString is not configured.");
var openIddictIssuer = builder.Configuration["OpenIddict:Issuer"]
    ?? throw new InvalidOperationException("OpenIddict:Issuer is not configured.");

// JWT Options
builder.Services.Configure<OAuth.Infrastructure.Options.JwtOptions>(builder.Configuration.GetSection(OAuth.Infrastructure.Options.JwtOptions.SectionName));

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    options.UseOpenIddict();
});

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
});

// OpenIddict
builder.Services.AddOpenIddict()

    // Register the OpenIddict core components.
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })

    // Register the OpenIddict server components.
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetUserinfoEndpointUris("/connect/userinfo")
               .SetLogoutEndpointUris("/connect/logout")
               .SetIntrospectionEndpointUris("/connect/introspect");

        options.RegisterScopes("openid", "profile", "email", "phone");

        // Authorization Code Flow with PKCE
        options.AllowAuthorizationCodeFlow();

        // Signing and encryption
        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey();

        // Accepted issuer
        options.SetIssuer(openIddictIssuer);

        options.AddEventHandler<OpenIddictServerEvents.ProcessSignInContext>(builder =>
        {
            builder.UseInlineHandler(context =>
            {
                return default;
            });
        });
    })

    // Register the OpenIddict validation components.
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<OAuth.Infrastructure.Options.JwtOptions>()
        ?? throw new InvalidOperationException("Jwt configuration not found.");

    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
    };
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, "Admin", "SuperAdmin", "Operator");
    });
    options.AddPolicy("SuperAdminOnly", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, "SuperAdmin");
    });
});

// HttpClient
builder.Services.AddHttpClient();

// Infrastructure services
builder.Services.AddInfrastructure();
builder.Services.AddScoped<IJwtService, OAuth.Infrastructure.Services.JwtService>();

// Configuration options
builder.Services.Configure<OAuth.Infrastructure.Options.TokenOptions>(builder.Configuration.GetSection(OAuth.Infrastructure.Options.TokenOptions.SectionName));
builder.Services.Configure<OAuth.Infrastructure.Options.UploadOptions>(builder.Configuration.GetSection(OAuth.Infrastructure.Options.UploadOptions.SectionName));
builder.Services.Configure<OAuth.Infrastructure.Options.SecurityOptions>(builder.Configuration.GetSection(OAuth.Infrastructure.Options.SecurityOptions.SectionName));

// Controllers
builder.Services.AddControllers(options =>
{
    options.Filters.Add<OAuth.Server.Filters.ValidateModelAttribute>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Disable default model validation response
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new Microsoft.AspNetCore.Mvc.Versioning.UrlSegmentApiVersionReader();
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedAsync(context);
    Console.WriteLine("Database seeded successfully!");
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Global Exception Handler
app.UseGlobalExceptionHandler();

app.UseStaticFiles();

app.UseCors();

app.UseRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();