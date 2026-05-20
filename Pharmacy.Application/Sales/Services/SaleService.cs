using FluentValidation;
using Pharmacy.Application.Common.Exceptions;
using Pharmacy.Application.Common.Interfaces;
using Pharmacy.Application.Common.Models;
using Pharmacy.Application.Common.Validation;
using Pharmacy.Application.Sales.Contracts;
using Pharmacy.Application.Sales.DTOs;
using Pharmacy.Application.Sales.Interfaces;
using Pharmacy.Domain.Entities;
using Pharmacy.Domain.Interfaces;

namespace Pharmacy.Application.Sales.Services;

public sealed class SaleService : ISaleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISaleReadRepository _saleReadRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateSaleDto> _createValidator;
    private readonly IValidator<GetSalesQueryDto> _getPagedValidator;

    public SaleService(
        IUnitOfWork unitOfWork,
        ISaleReadRepository saleReadRepository,
        ICurrentUserService currentUserService,
        IValidator<CreateSaleDto> createValidator,
        IValidator<GetSalesQueryDto> getPagedValidator)
    {
        _unitOfWork = unitOfWork;
        _saleReadRepository = saleReadRepository;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
        _getPagedValidator = getPagedValidator;
    }

    public async Task<CreateSaleResponse> CreateAsync(
        CreateSaleDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(dto, _createValidator, cancellationToken);

        var userId = _currentUserService.GetUserId();
        var saleRepository = _unitOfWork.GetRepository<Sale>();
        var medicineRepository = _unitOfWork.GetRepository<Medicine>();

        var saleItems = new List<SaleItem>();
        decimal totalAmount = 0;

        foreach (var item in dto.Items)
        {
            var medicine = await medicineRepository.GetByIdForUpdateAsync(
                item.MedicineId,
                cancellationToken);

            if (medicine is null)
                throw new NotFoundException(nameof(Medicine), item.MedicineId);

            if (!medicine.IsActive)
            {
                throw new ConflictException(
                    $"Medicine '{medicine.Name}' is not active and cannot be sold.");
            }

            if (medicine.Stock < item.Quantity)
            {
                throw new ConflictException(
                    $"Insufficient stock for '{medicine.Name}'. Available: {medicine.Stock}, requested: {item.Quantity}.");
            }

            var unitPrice = medicine.Price;
            totalAmount += unitPrice * item.Quantity;
            medicine.Stock -= item.Quantity;

            saleItems.Add(new SaleItem
            {
                MedicineId = medicine.Id,
                Quantity = item.Quantity,
                UnitPrice = unitPrice
            });
        }

        var sale = new Sale
        {
            SaleDate = DateTime.UtcNow,
            UserId = userId,
            TotalAmount = totalAmount,
            SaleItems = saleItems
        };

        await saleRepository.AddAsync(sale, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateSaleResponse(sale.Id, sale.TotalAmount, sale.SaleDate);
    }

    public async Task<PagedResponse<SaleListItemDto>> GetPagedAsync(
        GetSalesQueryDto query,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(query, _getPagedValidator, cancellationToken);

        var totalCount = await _saleReadRepository.CountAsync(cancellationToken);
        var sales = await _saleReadRepository.GetPagedAsync(
            query.Skip,
            query.Take,
            cancellationToken);

        var items = new List<SaleListItemDto>();

        foreach (var sale in sales)
        {
            var saleItems = await _saleReadRepository.GetItemsBySaleIdAsync(sale.Id, cancellationToken);

            items.Add(new SaleListItemDto(
                sale.Id,
                sale.SaleDate,
                sale.TotalAmount,
                sale.UserId,
                saleItems.Count));
        }

        return new PagedResponse<SaleListItemDto>(items, query.Skip, query.Take, totalCount);
    }

    public async Task<SaleDetailsDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateId(id);

        var saleRepository = _unitOfWork.GetRepository<Sale>();
        var medicineRepository = _unitOfWork.GetRepository<Medicine>();

        var sale = await saleRepository.GetByIdAsync(id, cancellationToken);

        if (sale is null)
            throw new NotFoundException(nameof(Sale), id);

        var saleItems = await _saleReadRepository.GetItemsBySaleIdAsync(id, cancellationToken);

        var lineItems = new List<SaleLineItemDto>();

        foreach (var item in saleItems)
        {
            var medicine = await medicineRepository.GetByIdAsync(item.MedicineId, cancellationToken);

            lineItems.Add(new SaleLineItemDto(
                item.MedicineId,
                medicine?.Name ?? "Unknown",
                item.Quantity,
                item.UnitPrice,
                item.UnitPrice * item.Quantity));
        }

        return new SaleDetailsDto(
            sale.Id,
            sale.SaleDate,
            sale.TotalAmount,
            sale.UserId,
            lineItems);
    }
}
