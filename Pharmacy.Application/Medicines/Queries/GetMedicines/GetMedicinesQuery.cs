using MediatR;
using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Medicines.Contracts;

namespace Pharmacy.Application.Medicines.Queries.GetMedicines;

public sealed record GetMedicinesQuery(int Skip, int Take)
    : IRequest<PagedResponse<MedicineListItemDto>>;
