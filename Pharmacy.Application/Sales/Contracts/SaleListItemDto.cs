namespace Pharmacy.Application.Sales.Contracts;

public sealed record SaleListItemDto(
    int Id,
    DateTime SaleDate,
    decimal TotalAmount,
    int UserId,
    int ItemCount);
