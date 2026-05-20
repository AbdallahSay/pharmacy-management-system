namespace Pharmacy.Application.Sales.Contracts;

public sealed record CreateSaleResponse(int Id, decimal TotalAmount, DateTime SaleDate);
