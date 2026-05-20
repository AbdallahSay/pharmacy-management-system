using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Medicines.Contracts;
using Pharmacy.Application.Medicines.DTOs;

namespace Pharmacy.Application.Medicines.Interfaces;

public interface IMedicineService
{
    Task<CreateMedicineResponse> CreateAsync(
        CreateMedicineDto dto,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<MedicineListItemDto>> GetPagedAsync(
        GetMedicinesQueryDto query,
        CancellationToken cancellationToken = default);

    Task<MedicineDetailsDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int id,
        UpdateMedicineDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}
