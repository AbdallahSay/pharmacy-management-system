using Pharmacy.Application.Common.Interfaces;

namespace Pharmacy.API.Services;

public sealed class TenantContext : ITenantContext
{
    public int TenantId { get; private set; }
    public bool IsResolved { get; private set; }

    public void SetTenant(int tenantId)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId));

        TenantId = tenantId;
        IsResolved = true;
    }
}
