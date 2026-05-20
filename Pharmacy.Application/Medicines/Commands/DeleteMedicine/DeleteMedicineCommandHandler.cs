using MediatR;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Domain.Entities;
using Pharmacy.Domain.Interfaces;

namespace Pharmacy.Application.Medicines.Commands.DeleteMedicine;

public sealed class DeleteMedicineCommandHandler : IRequestHandler<DeleteMedicineCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMedicineCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteMedicineCommand request, CancellationToken cancellationToken)
    {
        var medicineRepository = _unitOfWork.GetRepository<Medicine>();

        if (!await medicineRepository.ExistsAsync(request.Id, cancellationToken))
            throw new NotFoundException(nameof(Medicine), request.Id);

        var deleted = await medicineRepository.DeleteAsync(request.Id, cancellationToken);

        if (!deleted)
            throw new NotFoundException(nameof(Medicine), request.Id);
    }
}
