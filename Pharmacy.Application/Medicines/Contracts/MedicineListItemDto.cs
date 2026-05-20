namespace Pharmacy.Application.Medicines.Contracts;

public sealed record MedicineListItemDto(
    int Id,
    string Name,
    decimal Price,
    int Stock,
    int CategoryId,
    string? CategoryName,
    bool IsActive,
    DateTime ExpiryDate);
