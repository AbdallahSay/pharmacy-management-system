namespace Pharmacy.Application.Medicines.DTOs;

/// <summary>
/// API input contract. Only whitelisted fields — prevents overposting of entity properties.
/// </summary>
public sealed class CreateMedicineDto
{
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public int CategoryId { get; init; }
}
