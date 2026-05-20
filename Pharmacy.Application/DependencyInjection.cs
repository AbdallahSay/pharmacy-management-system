using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Application.Categories.Interfaces;
using Pharmacy.Application.Categories.Services;
using Pharmacy.Application.Medicines.Interfaces;
using Pharmacy.Application.Medicines.Services;
using System.Reflection;

namespace Pharmacy.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IMedicineService, MedicineService>();
        services.AddScoped<ICategoryService, CategoryService>();

        return services;
    }
}
