using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Application.Auth.Contracts;
using Pharmacy.Application.Auth.DTOs;
using Pharmacy.Domain.Entities;
using Pharmacy.Infrastructure.Auth;

namespace Pharmacy.IntegrationTests.TestInfrastructure;

public sealed class AuthTestClient
{
    private readonly PharmacyApiFactory _factory;

    public AuthTestClient(PharmacyApiFactory factory)
    {
        _factory = factory;
    }

    public async Task<HttpClient> ForUserAsync(TestUser user, string tenantSlug)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = user.Email,
            Password = user.Password,
            TenantSlug = tenantSlug
        });

        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>()
            ?? throw new InvalidOperationException("Login did not return an auth response.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    public HttpClient WithForgedToken(int userId, int tenantId, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<JwtTokenGenerator>();
        var user = new ApplicationUser
        {
            Id = userId,
            TenantId = tenantId,
            Email = "forged@example.test",
            FullName = "Forged User"
        };

        var (token, _) = tokenGenerator.Generate(user, [role], tenantId);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
