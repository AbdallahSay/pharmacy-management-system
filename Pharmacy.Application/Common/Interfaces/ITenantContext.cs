namespace Pharmacy.Application.Common.Interfaces;

public interface ITenantContext
{
    int TenantId { get; }
    bool IsResolved { get; }
    void SetTenant(int tenantId);
}
