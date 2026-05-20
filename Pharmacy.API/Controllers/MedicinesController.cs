using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Medicines.Commands.CreateMedicine;
using Pharmacy.Application.Medicines.Contracts;
using Pharmacy.Application.Medicines.DTOs;
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
            nameof(Create),
            new { id = result.Id },
            result);
    }
}
