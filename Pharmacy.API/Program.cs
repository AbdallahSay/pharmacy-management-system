using Pharmacy.API.Extensions;
using Pharmacy.API.Middleware;
using Pharmacy.Application;
using Pharmacy.Infrastructure;
using Scalar.AspNetCore;

namespace Pharmacy.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApiServices();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi("Pharmacy" , options => 
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        var app = builder.Build();

        await app.ApplyMigrationsAsync();
        await app.SeedIdentityAsync();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.Title = "Pharmacy API";
                options.OpenApiRoutePattern = "/openapi/Pharmacy.json";
            });
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseMiddleware<TenantResolutionMiddleware>();
        app.UseAuthorization();
        app.MapControllers();

        await app.RunAsync();
    }
}
