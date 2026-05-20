using FluentValidation;
using Pharmacy.Application.Categories.Contracts;
using Pharmacy.Application.Categories.DTOs;
using Pharmacy.Application.Categories.Interfaces;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Common.Validation;
using Pharmacy.Domain.Entities;
using Pharmacy.Domain.Interfaces;

namespace Pharmacy.Application.Categories.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCategoryDto> _createValidator;
    private readonly IValidator<UpdateCategoryDto> _updateValidator;
    private readonly IValidator<GetCategoriesQueryDto> _getPagedValidator;

    public CategoryService(
        IUnitOfWork unitOfWork,
        IValidator<CreateCategoryDto> createValidator,
        IValidator<UpdateCategoryDto> updateValidator,
        IValidator<GetCategoriesQueryDto> getPagedValidator)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _getPagedValidator = getPagedValidator;
    }

    public async Task<CreateCategoryResponse> CreateAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(dto, _createValidator, cancellationToken);

        var categoryRepository = _unitOfWork.GetRepository<Category>();

        await EnsureNameIsUniqueAsync(categoryRepository, dto.Name, cancellationToken);

        var category = new Category
        {
            Name = dto.Name.Trim()
        };

        await categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateCategoryResponse(category.Id);
    }

    public async Task<PagedResponse<CategoryListItemDto>> GetPagedAsync(
        GetCategoriesQueryDto query,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(query, _getPagedValidator, cancellationToken);

        var categoryRepository = _unitOfWork.GetRepository<Category>();

        var totalCount = await categoryRepository.CountAsync(cancellationToken);
        var categories = await categoryRepository.ListAsync(query.Skip, query.Take, cancellationToken);

        var items = categories
            .Select(c => new CategoryListItemDto(c.Id, c.Name))
            .ToList();

        return new PagedResponse<CategoryListItemDto>(items, query.Skip, query.Take, totalCount);
    }

    public async Task<CategoryDetailsDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateId(id);

        var categoryRepository = _unitOfWork.GetRepository<Category>();

        var category = await categoryRepository.GetByIdAsync(id, cancellationToken);

        if (category is null)
            throw new NotFoundException(nameof(Category), id);

        return new CategoryDetailsDto(category.Id, category.Name, category.CreatedAt);
    }

    public async Task UpdateAsync(
        int id,
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateId(id);
        await ValidationHelper.ValidateAndThrowAsync(dto, _updateValidator, cancellationToken);

        var categoryRepository = _unitOfWork.GetRepository<Category>();

        var category = await categoryRepository.GetByIdForUpdateAsync(id, cancellationToken);

        if (category is null)
            throw new NotFoundException(nameof(Category), id);

        await EnsureNameIsUniqueAsync(categoryRepository, dto.Name, cancellationToken, id);

        category.Name = dto.Name.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateId(id);

        var categoryRepository = _unitOfWork.GetRepository<Category>();
        var medicineRepository = _unitOfWork.GetRepository<Medicine>();

        if (!await categoryRepository.ExistsAsync(id, cancellationToken))
            throw new NotFoundException(nameof(Category), id);

        if (await HasAssociatedMedicinesAsync(medicineRepository, id, cancellationToken))
        {
            throw new ConflictException(
                "Cannot delete this category because it is referenced by existing medicines.");
        }

        var deleted = await categoryRepository.DeleteAsync(id, cancellationToken);

        if (!deleted)
            throw new NotFoundException(nameof(Category), id);
    }

    private static async Task EnsureNameIsUniqueAsync(
        IRepository<Category> categoryRepository,
        string name,
        CancellationToken cancellationToken,
        int? excludeCategoryId = null)
    {
        var normalizedName = name.Trim();
        var totalCount = await categoryRepository.CountAsync(cancellationToken);

        if (totalCount == 0)
            return;

        var categories = await categoryRepository.ListAsync(0, totalCount, cancellationToken);

        var duplicateExists = categories.Any(c =>
            c.Id != excludeCategoryId &&
            string.Equals(c.Name, normalizedName, StringComparison.OrdinalIgnoreCase));

        if (duplicateExists)
        {
            throw new ConflictException($"A category with name '{normalizedName}' already exists.");
        }
    }

    private static async Task<bool> HasAssociatedMedicinesAsync(
        IRepository<Medicine> medicineRepository,
        int categoryId,
        CancellationToken cancellationToken)
    {
        var totalMedicines = await medicineRepository.CountAsync(cancellationToken);

        if (totalMedicines == 0)
            return false;

        var medicines = await medicineRepository.ListAsync(0, totalMedicines, cancellationToken);

        return medicines.Any(m => m.CategoryId == categoryId);
    }
}
