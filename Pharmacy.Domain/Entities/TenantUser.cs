namespace Pharmacy.Domain.Entities;

public class TenantUser
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public Tenant Tenant { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
