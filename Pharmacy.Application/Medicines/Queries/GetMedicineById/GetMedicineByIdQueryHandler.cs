using MediatR;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Application.Medicines.Contracts;
using Pharmacy.Domain.Entities;
using Pharmacy.Domain.Interfaces;

namespace Pharmacy.Application.Medicines.Queries.GetMedicineById;

public sealed class GetMedicineByIdQueryHandler
    : IRequestHandler<GetMedicineByIdQuery, MedicineDetailsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMedicineByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MedicineDetailsDto> Handle(
        GetMedicineByIdQuery request,
        CancellationToken cancellationToken)
    {
        var medicineRepository = _unitOfWork.GetRepository<Medicine>();
        var categoryRepository = _unitOfWork.GetRepository<Category>();

        var medicine = await medicineRepository.GetByIdAsync(request.Id, cancellationToken);

        if (medicine is null)
            throw new NotFoundException(nameof(Medicine), request.Id);

        var category = await categoryRepository.GetByIdAsync(medicine.CategoryId, cancellationToken);

        return new MedicineDetailsDto(
            medicine.Id,
            medicine.Name,
            medicine.Price,
            medicine.Description,
            medicine.Stock,
            medicine.MinStock,
            medicine.ExpiryDate,
            medicine.IsActive,
            medicine.CategoryId,
            category?.Name,
            medicine.CreatedAt);
    }
}
