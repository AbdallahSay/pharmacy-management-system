using Pharmacy.Application.Categories.Contracts;
using Pharmacy.Application.Categories.DTOs;
using Pharmacy.Application.Common.Models;

namespace Pharmacy.Application.Categories.Interfaces;

public interface ICategoryService
{
    Task<CreateCategoryResponse> CreateAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<CategoryListItemDto>> GetPagedAsync(
        GetCategoriesQueryDto query,
        CancellationToken cancellationToken = default);

    Task<CategoryDetailsDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int id,
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}
