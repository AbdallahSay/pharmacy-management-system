namespace Pharmacy.Application.Sales.Contracts;

public sealed record SaleDetailsDto(
    int Id,
    DateTime SaleDate,
    decimal TotalAmount,
    int UserId,
    IReadOnlyList<SaleLineItemDto> Items);
