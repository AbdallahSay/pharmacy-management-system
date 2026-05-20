namespace Pharmacy.Application.Medicines.DTOs;

/// <summary>
/// Whitelisted updatable fields only. Id and CreatedAt are never accepted from the client.
/// </summary>
public sealed class UpdateMedicineDto
{
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string? Description { get; init; }
    public int Stock { get; init; }
    public int MinStock { get; init; }
    public DateTime ExpiryDate { get; init; }
    public bool IsActive { get; init; }
    public int CategoryId { get; init; }
}
