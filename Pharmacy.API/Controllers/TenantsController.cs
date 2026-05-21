using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Application.Common.Constants;
using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Tenants.Contracts;
using Pharmacy.Application.Tenants.DTOs;
using Pharmacy.Application.Tenants.Interfaces;

namespace Pharmacy.API.Controllers;

/// <summary>
/// Platform-admin operations for managing pharmacy tenants.
/// Creating a tenant provisions the pharmacy + its first TenantAdmin user in one call.
/// </summary>
[Authorize(Policy = AuthPolicies.PlatformAdminOnly)]
[ApiController]
[Route("api/[controller]")]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// Create a new pharmacy tenant with its first TenantAdmin user.
    /// After this call the TenantAdmin can login using their email + password + the slug.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TenantCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TenantCreatedResponse>> Create(
        [FromBody] CreateTenantDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _tenantService.CreateAsync(dto, cancellationToken);

        return CreatedAtAction(
            nameof(GetAll),
            new { },
            result);
    }

    /// <summary>
    /// List all pharmacy tenants (paginated).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TenantListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<TenantListItemDto>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _tenantService.GetPagedAsync(skip, take, cancellationToken);
        return Ok(result);
    }
}
