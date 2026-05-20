using MediatR;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Application.Medicines.Contracts;
using Pharmacy.Domain.Entities;
using Pharmacy.Domain.Interfaces;

namespace Pharmacy.Application.Medicines.Commands.CreateMedicine;

public sealed class CreateMedicineCommandHandler
    : IRequestHandler<CreateMedicineCommand, CreateMedicineResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateMedicineCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateMedicineResponse> Handle(
        CreateMedicineCommand request,
        CancellationToken cancellationToken)
    {
        var categoryRepository = _unitOfWork.GetRepository<Category>();
        var medicineRepository = _unitOfWork.GetRepository<Medicine>();

        if (!await categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
            throw new NotFoundException(nameof(Category), request.CategoryId);

        var medicine = new Medicine
        {
            Name = request.Name.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            CategoryId = request.CategoryId,
            IsActive = true,
            MinStock = 0,
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        };

        await medicineRepository.AddAsync(medicine, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateMedicineResponse(medicine.Id);
    }
}
