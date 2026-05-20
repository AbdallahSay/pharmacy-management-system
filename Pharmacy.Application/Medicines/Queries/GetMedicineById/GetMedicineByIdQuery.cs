using MediatR;
using Pharmacy.Application.Medicines.Contracts;

namespace Pharmacy.Application.Medicines.Queries.GetMedicineById;

public sealed record GetMedicineByIdQuery(int Id) : IRequest<MedicineDetailsDto>;
