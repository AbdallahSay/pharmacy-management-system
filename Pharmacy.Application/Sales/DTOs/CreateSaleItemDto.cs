namespace Pharmacy.Application.Sales.DTOs;

public sealed class CreateSaleItemDto
{
    public int MedicineId { get; init; }
    public int Quantity { get; init; }
}
