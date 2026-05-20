using Pharmacy.Domain.Common;

namespace Pharmacy.Application.Sales.DTOs;

public sealed class GetSalesQueryDto
{
    public int Skip { get; init; }

    public int Take { get; init; } = RepositoryLimits.DefaultPageSize;
}
