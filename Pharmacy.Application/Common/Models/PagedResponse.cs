namespace Pharmacy.Application.Common.Models;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Skip,
    int Take,
    int TotalCount);
