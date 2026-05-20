using Pharmacy.Domain.Common;

namespace Pharmacy.Application.Categories.DTOs;

public sealed class GetCategoriesQueryDto
{
    public int Skip { get; init; }

    public int Take { get; init; } = RepositoryLimits.DefaultPageSize;
}
