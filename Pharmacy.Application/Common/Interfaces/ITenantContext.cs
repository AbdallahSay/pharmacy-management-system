namespace Pharmacy.Application.Common.Interfaces;

public interface ITenantContext
{
    int TenantId { get; }
    bool IsResolved { get; }
    bool IsBypassed { get; }
    void SetTenant(int tenantId);
    IDisposable BeginBypass();
}
