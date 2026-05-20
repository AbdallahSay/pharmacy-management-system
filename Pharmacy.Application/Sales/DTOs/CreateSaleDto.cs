namespace Pharmacy.Application.Sales.DTOs;

public sealed class CreateSaleDto
{
    public IReadOnlyList<CreateSaleItemDto> Items { get; init; } = Array.Empty<CreateSaleItemDto>();
}
