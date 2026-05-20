using MediatR;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Domain.Entities;
using Pharmacy.Domain.Interfaces;

namespace Pharmacy.Application.Medicines.Commands.UpdateMedicine;

public sealed class UpdateMedicineCommandHandler : IRequestHandler<UpdateMedicineCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMedicineCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateMedicineCommand request, CancellationToken cancellationToken)
    {
        var medicineRepository = _unitOfWork.GetRepository<Medicine>();
        var categoryRepository = _unitOfWork.GetRepository<Category>();

        var medicine = await medicineRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (medicine is null)
            throw new NotFoundException(nameof(Medicine), request.Id);

        if (!await categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
            throw new NotFoundException(nameof(Category), request.CategoryId);

        medicine.Name = request.Name.Trim();
        medicine.Price = request.Price;
        medicine.Description = request.Description;
        medicine.Stock = request.Stock;
        medicine.MinStock = request.MinStock;
        medicine.ExpiryDate = request.ExpiryDate;
        medicine.IsActive = request.IsActive;
        medicine.CategoryId = request.CategoryId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
