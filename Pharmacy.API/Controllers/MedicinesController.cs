using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Medicines.Commands.CreateMedicine;
using Pharmacy.Application.Medicines.Commands.UpdateMedicine;
using Pharmacy.Application.Medicines.Contracts;
using Pharmacy.Application.Medicines.DTOs;
using Pharmacy.Application.Medicines.Queries.GetMedicineById;
using Pharmacy.Application.Medicines.Queries.GetMedicines;

namespace Pharmacy.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicinesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MedicinesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<MedicineListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<MedicineListItemDto>>> GetAll(
        [FromQuery] GetMedicinesQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMedicinesQuery(query.Skip, query.Take),
            cancellationToken);

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
        var result = await _mediator.Send(new GetMedicineByIdQuery(id), cancellationToken);
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
        var command = new CreateMedicineCommand(
            dto.Name,
            dto.Price,
            dto.Stock,
            dto.CategoryId);

        var result = await _mediator.Send(command, cancellationToken);

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
        await _mediator.Send(
            new UpdateMedicineCommand(
                id,
                dto.Name,
                dto.Price,
                dto.Description,
                dto.Stock,
                dto.MinStock,
                dto.ExpiryDate,
                dto.IsActive,
                dto.CategoryId),
            cancellationToken);

        return NoContent();
    }
}
