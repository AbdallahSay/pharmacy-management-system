using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Application.Common.Constants;
using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Medicines.Contracts;
using Pharmacy.Application.Medicines.DTOs;
using Pharmacy.Application.Medicines.Interfaces;

namespace Pharmacy.API.Controllers;

[Authorize(Policy = AuthPolicies.PharmacyStaff)]
[ApiController]
[Route("api/[controller]")]
public class MedicinesController : ControllerBase
{
    private readonly IMedicineService _medicineService;

    public MedicinesController(IMedicineService medicineService)
    {
        _medicineService = medicineService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<MedicineListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<MedicineListItemDto>>> GetAll(
        [FromQuery] GetMedicinesQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _medicineService.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MedicineDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicineDetailsDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _medicineService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateMedicineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateMedicineResponse>> Create(
        [FromBody] CreateMedicineDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _medicineService.CreateAsync(dto, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateMedicineDto dto,
        CancellationToken cancellationToken)
    {
        await _medicineService.UpdateAsync(id, dto, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _medicineService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
