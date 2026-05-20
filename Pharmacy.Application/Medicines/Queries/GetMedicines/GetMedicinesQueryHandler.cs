using MediatR;
using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Medicines.Contracts;
using Pharmacy.Domain.Entities;
using Pharmacy.Domain.Interfaces;

namespace Pharmacy.Application.Medicines.Queries.GetMedicines;

public sealed class GetMedicinesQueryHandler
    : IRequestHandler<GetMedicinesQuery, PagedResponse<MedicineListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMedicinesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<MedicineListItemDto>> Handle(
        GetMedicinesQuery request,
        CancellationToken cancellationToken)
    {
        var medicineRepository = _unitOfWork.GetRepository<Medicine>();
        var categoryRepository = _unitOfWork.GetRepository<Category>();

        var totalCount = await medicineRepository.CountAsync(cancellationToken);
        var medicines = await medicineRepository.ListAsync(
            request.Skip,
            request.Take,
            cancellationToken);

        var categoryNames = await ResolveCategoryNamesAsync(
            medicines,
            categoryRepository,
            cancellationToken);

        var items = medicines
            .Select(m => new MedicineListItemDto(
                m.Id,
                m.Name,
                m.Price,
                m.Stock,
                m.CategoryId,
                categoryNames.GetValueOrDefault(m.CategoryId),
                m.IsActive,
                m.ExpiryDate))
            .ToList();

        return new PagedResponse<MedicineListItemDto>(
            items,
            request.Skip,
            request.Take,
            totalCount);
    }

    private static async Task<IReadOnlyDictionary<int, string>> ResolveCategoryNamesAsync(
        IReadOnlyList<Medicine> medicines,
        IRepository<Category> categoryRepository,
        CancellationToken cancellationToken)
    {
        var categoryIds = medicines
            .Select(m => m.CategoryId)
            .Distinct();

        var categoryNames = new Dictionary<int, string>();

        foreach (var categoryId in categoryIds)
        {
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is not null)
                categoryNames[categoryId] = category.Name;
        }

        return categoryNames;
    }
}
