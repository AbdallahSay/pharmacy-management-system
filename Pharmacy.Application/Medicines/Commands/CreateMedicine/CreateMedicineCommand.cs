using MediatR;
using Pharmacy.Application.Medicines.Contracts;

namespace Pharmacy.Application.Medicines.Commands.CreateMedicine;

public sealed record CreateMedicineCommand(
    string Name,
    decimal Price,
    int Stock,
    int CategoryId) : IRequest<CreateMedicineResponse>;
