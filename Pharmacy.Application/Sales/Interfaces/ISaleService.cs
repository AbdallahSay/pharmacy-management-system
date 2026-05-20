using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Sales.Contracts;
using Pharmacy.Application.Sales.DTOs;

namespace Pharmacy.Application.Sales.Interfaces;

public interface ISaleService
{
    Task<CreateSaleResponse> CreateAsync(
        CreateSaleDto dto,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<SaleListItemDto>> GetPagedAsync(
        GetSalesQueryDto query,
        CancellationToken cancellationToken = default);

    Task<SaleDetailsDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
}
