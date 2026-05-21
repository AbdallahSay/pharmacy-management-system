using Pharmacy.API.Extensions;
using Pharmacy.API.Middleware;
using Pharmacy.Application;
using Pharmacy.Infrastructure;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;

namespace Pharmacy.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddControllers();
        builder.Services.AddOpenApi("Pharmacy" , options => 
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("loginPolicy", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(5);
                opt.PermitLimit = 5; // Max 5 login attempts per 5 minutes per IP
            });

            options.AddFixedWindowLimiter("refreshPolicy", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 10; // Max 10 refresh attempts per minute
            });
        });

        var app = builder.Build();

        await app.ApplyMigrationsAsync();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.Title = "Pharmacy API";
                options.OpenApiRoutePattern = "/openapi/Pharmacy.json";
            });
        }

        app.UseRateLimiter();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        await app.RunAsync();
    }
}
