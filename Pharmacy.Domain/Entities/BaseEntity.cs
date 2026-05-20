namespace Pharmacy.Domain.Entities;

public abstract class BaseEntity : ITenantEntity, ISoftDeletable
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
