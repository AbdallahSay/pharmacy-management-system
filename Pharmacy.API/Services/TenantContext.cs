using Pharmacy.Application.Common.Interfaces;

namespace Pharmacy.API.Services;

public sealed class TenantContext : ITenantContext
{
    private int _bypassDepth;

    public int TenantId { get; private set; }
    public bool IsResolved { get; private set; }
    public bool IsBypassed => _bypassDepth > 0;

    public void SetTenant(int tenantId)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId));

        TenantId = tenantId;
        IsResolved = true;
    }

    public IDisposable BeginBypass()
    {
        _bypassDepth++;
        return new BypassScope(this);
    }

    private void EndBypass()
    {
        if (_bypassDepth > 0)
            _bypassDepth--;
    }

    private sealed class BypassScope : IDisposable
    {
        private readonly TenantContext _tenantContext;
        private bool _disposed;

        public BypassScope(TenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _tenantContext.EndBypass();
            _disposed = true;
        }
    }
}
