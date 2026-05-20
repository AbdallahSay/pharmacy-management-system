namespace Pharmacy.Application.Medicines.Contracts;

public sealed record MedicineDetailsDto(
    int Id,
    string Name,
    decimal Price,
    string? Description,
    int Stock,
    int MinStock,
    DateTime ExpiryDate,
    bool IsActive,
    int CategoryId,
    string? CategoryName,
    DateTime CreatedAt);
