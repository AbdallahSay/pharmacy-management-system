namespace Pharmacy.Domain.Entities
{
    public class SaleItem : BaseEntity
    {
        public int SaleId { get; set; }
        public Sale Sale { get; set; } = null!;
        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        
    }
}