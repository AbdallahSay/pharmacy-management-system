using MediatR;

namespace Pharmacy.Application.Medicines.Commands.DeleteMedicine;

public sealed record DeleteMedicineCommand(int Id) : IRequest;
