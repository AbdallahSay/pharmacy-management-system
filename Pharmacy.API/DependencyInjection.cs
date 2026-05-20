using Pharmacy.API.Services;
using Pharmacy.Application.Common.Interfaces;

namespace Pharmacy.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITenantContext, TenantContext>();

        return services;
    }
}
