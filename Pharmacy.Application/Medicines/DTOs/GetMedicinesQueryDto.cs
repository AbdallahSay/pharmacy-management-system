using Pharmacy.Domain.Common;

namespace Pharmacy.Application.Medicines.DTOs;

/// <summary>
/// Query-string binding contract for GET /api/medicines.
/// </summary>
public sealed class GetMedicinesQueryDto
{
    public int Skip { get; init; }

    public int Take { get; init; } = RepositoryLimits.DefaultPageSize;
}
