using FluentValidation;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Common.Validation;
using Pharmacy.Application.Medicines.Contracts;
using Pharmacy.Application.Medicines.DTOs;
using Pharmacy.Application.Medicines.Interfaces;
using Pharmacy.Domain.Entities;
using Pharmacy.Domain.Interfaces;

namespace Pharmacy.Application.Medicines.Services;

public sealed class MedicineService : IMedicineService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateMedicineDto> _createValidator;
    private readonly IValidator<UpdateMedicineDto> _updateValidator;
    private readonly IValidator<GetMedicinesQueryDto> _getPagedValidator;

    public MedicineService(
        IUnitOfWork unitOfWork,
        IValidator<CreateMedicineDto> createValidator,
        IValidator<UpdateMedicineDto> updateValidator,
        IValidator<GetMedicinesQueryDto> getPagedValidator)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _getPagedValidator = getPagedValidator;
    }

    public async Task<CreateMedicineResponse> CreateAsync(
        CreateMedicineDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(dto, _createValidator, cancellationToken);

        var categoryRepository = _unitOfWork.GetRepository<Category>();
        var medicineRepository = _unitOfWork.GetRepository<Medicine>();

        if (!await categoryRepository.ExistsAsync(dto.CategoryId, cancellationToken))
            throw new NotFoundException(nameof(Category), dto.CategoryId);

        var medicine = new Medicine
        {
            Name = dto.Name.Trim(),
            Price = dto.Price,
            Stock = dto.Stock,
            CategoryId = dto.CategoryId,
            IsActive = true,
            MinStock = 0,
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        };

        await medicineRepository.AddAsync(medicine, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateMedicineResponse(medicine.Id);
    }

    public async Task<PagedResponse<MedicineListItemDto>> GetPagedAsync(
        GetMedicinesQueryDto query,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(query, _getPagedValidator, cancellationToken);

        var medicineRepository = _unitOfWork.GetRepository<Medicine>();
        var categoryRepository = _unitOfWork.GetRepository<Category>();

        var totalCount = await medicineRepository.CountAsync(cancellationToken);
        var medicines = await medicineRepository.ListAsync(query.Skip, query.Take, cancellationToken);

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

        return new PagedResponse<MedicineListItemDto>(items, query.Skip, query.Take, totalCount);
    }

    public async Task<MedicineDetailsDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateId(id);

        var medicineRepository = _unitOfWork.GetRepository<Medicine>();
        var categoryRepository = _unitOfWork.GetRepository<Category>();

        var medicine = await medicineRepository.GetByIdAsync(id, cancellationToken);

        if (medicine is null)
            throw new NotFoundException(nameof(Medicine), id);

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

    public async Task UpdateAsync(
        int id,
        UpdateMedicineDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateId(id);
        await ValidationHelper.ValidateAndThrowAsync(dto, _updateValidator, cancellationToken);

        var medicineRepository = _unitOfWork.GetRepository<Medicine>();
        var categoryRepository = _unitOfWork.GetRepository<Category>();

        var medicine = await medicineRepository.GetByIdForUpdateAsync(id, cancellationToken);

        if (medicine is null)
            throw new NotFoundException(nameof(Medicine), id);

        if (!await categoryRepository.ExistsAsync(dto.CategoryId, cancellationToken))
            throw new NotFoundException(nameof(Category), dto.CategoryId);

        medicine.Name = dto.Name.Trim();
        medicine.Price = dto.Price;
        medicine.Description = dto.Description;
        medicine.Stock = dto.Stock;
        medicine.MinStock = dto.MinStock;
        medicine.ExpiryDate = dto.ExpiryDate;
        medicine.IsActive = dto.IsActive;
        medicine.CategoryId = dto.CategoryId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateId(id);

        var medicineRepository = _unitOfWork.GetRepository<Medicine>();

        if (!await medicineRepository.ExistsAsync(id, cancellationToken))
            throw new NotFoundException(nameof(Medicine), id);

        var deleted = await medicineRepository.DeleteAsync(id, cancellationToken);

        if (!deleted)
            throw new NotFoundException(nameof(Medicine), id);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<int, string>> ResolveCategoryNamesAsync(
        IReadOnlyList<Medicine> medicines,
        IRepository<Category> categoryRepository,
        CancellationToken cancellationToken)
    {
        var categoryNames = new Dictionary<int, string>();

        foreach (var categoryId in medicines.Select(m => m.CategoryId).Distinct())
        {
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is not null)
                categoryNames[categoryId] = category.Name;
        }

        return categoryNames;
    }
}
