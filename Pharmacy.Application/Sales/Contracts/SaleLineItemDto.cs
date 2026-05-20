namespace Pharmacy.Application.Sales.Contracts;

public sealed record SaleLineItemDto(
    int MedicineId,
    string MedicineName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
