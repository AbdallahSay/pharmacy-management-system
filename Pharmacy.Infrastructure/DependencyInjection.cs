using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Pharmacy.Application.Auth.Interfaces;
using Pharmacy.Application.Common.Constants;
using Pharmacy.Application.Common.Interfaces;
using Pharmacy.Application.Sales.Interfaces;
using Pharmacy.Application.Tenancy.Interfaces;
using Pharmacy.Application.Tenants.Interfaces;
using Pharmacy.Domain.Entities;
using Pharmacy.Domain.Interfaces;
using Pharmacy.Infrastructure.Auth;
using Pharmacy.Infrastructure.Persistence;
using Pharmacy.Infrastructure.Persistence.Interceptors;
using Pharmacy.Infrastructure.Tenancy;
using System.Text;

namespace Pharmacy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<PharmacyDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sql.MigrationsAssembly(typeof(PharmacyDbContext).Assembly.FullName);
            });

            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
            .AddEntityFrameworkStores<PharmacyDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<JwtTokenGenerator>();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings are not configured.");

        ArgumentException.ThrowIfNullOrWhiteSpace(jwtSettings.Key);
        ArgumentException.ThrowIfNullOrWhiteSpace(jwtSettings.Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(jwtSettings.Audience);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.PharmacyStaff, policy =>
                policy.RequireRole(RoleNames.Admin, RoleNames.Pharmacist));

            options.AddPolicy(AuthPolicies.AdminOnly, policy =>
                policy.RequireRole(RoleNames.Admin));
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, IdentityAuthService>();
        services.AddScoped<ISaleReadRepository, SaleReadRepository>();
        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddScoped<ITenantService, TenantService>();

        return services;
    }
}