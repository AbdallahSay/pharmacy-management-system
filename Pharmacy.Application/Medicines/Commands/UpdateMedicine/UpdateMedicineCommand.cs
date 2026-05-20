using MediatR;

namespace Pharmacy.Application.Medicines.Commands.UpdateMedicine;

public sealed record UpdateMedicineCommand(
    int Id,
    string Name,
    decimal Price,
    string? Description,
    int Stock,
    int MinStock,
    DateTime ExpiryDate,
    bool IsActive,
    int CategoryId) : IRequest;
